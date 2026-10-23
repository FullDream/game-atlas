using System.Net;
using System.Net.Http.Json;
using GameAtlas.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GameAtlas.IntegrationTests;

public class CategoryTransactionTests
{
	[Fact]
	public async Task ConcurrentAppendsAllocateDistinctOrdersAndSameNamesConflict()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		var path = CategoryTestData.Path(game.Id, group.Id);
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var tasks = Enumerable.Range(0, 3).Select(index => Task.Run(async () =>
		{
			await gate.Task;

			return await client.PostAsJsonAsync(path, CategoryApiTests.Body($"Category {index}"));
		})).ToArray();

		gate.SetResult();
		var responses = await Task.WhenAll(tasks);

		foreach (var response in responses)
		{
			using (response)
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		Assert.Equal(new[] { 1, 2, 3 },
			(await CategoryTestData.LoadAsync(factory, group.Id)).Select(item => item.Order));

		var duplicatesGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var duplicateTasks = new[] { "Ресурсы", " ресурсы " }.Select(name => Task.Run(async () =>
		{
			await duplicatesGate.Task;

			return await client.PostAsJsonAsync(path, CategoryApiTests.Body(name));
		})).ToArray();

		duplicatesGate.SetResult();
		var duplicates = await Task.WhenAll(duplicateTasks);

		Assert.Single(duplicates, response => response.StatusCode == HttpStatusCode.OK);
		Assert.Single(duplicates, response => response.StatusCode == HttpStatusCode.Conflict);

		foreach (var response in duplicates)
			response.Dispose();
	}

	[Theory]
	[InlineData("create")]
	[InlineData("update")]
	[InlineData("transfer")]
	[InlineData("delete")]
	public async Task FailureAfterDatabaseWriteRollsBackBeforeCommit(string operation)
	{
		var interceptor = new FailAfterSave();
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var destination = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		var before = await CategoryTestData.LoadAsync(factory, group.Id);
		var beforeDestination = await CategoryTestData.LoadAsync(factory, destination.Id);
		using var client = factory.CreateHttpsClient();
		var path = CategoryTestData.Path(game.Id, group.Id, category.Id);

		interceptor.Enabled = true;
		using var response = operation switch
		{
			"create" => await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, group.Id),
				CategoryApiTests.Body("New")),
			"update" => await client.PatchAsJsonAsync(path, new { name = "New", order = 20 }),
			"transfer" => await client.PatchAsJsonAsync(path,
				new { categoryGroupId = destination.Id, name = "New", order = 20 }),
			_ => await client.DeleteAsync(path)
		};

		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		Assert.DoesNotContain("private category write failure", body);
		Assert.DoesNotContain("SQLite", body);
		Assert.DoesNotContain("InvalidOperationException", body);
		Assert.Equal(before, await CategoryTestData.LoadAsync(factory, group.Id));
		Assert.Equal(beforeDestination, await CategoryTestData.LoadAsync(factory, destination.Id));
	}

	[Theory]
	[InlineData("create")]
	[InlineData("rename")]
	[InlineData("transfer")]
	public async Task ConstraintFailureDuringSavingIs409AndAtomic(string operation)
	{
		var interceptor = new ForceNameConflict();
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var (game, source) = await CategoryTestData.ParentAsync(factory);
		var destination = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);

		await CategoryTestData.SeedAsync(factory, source.Id, "Taken");
		await CategoryTestData.SeedAsync(factory, destination.Id, "Taken");
		var category = await CategoryTestData.SeedAsync(factory, source.Id, "Target", 2);
		var before = await CategoryTestData.LoadAsync(factory, source.Id);
		var beforeDestination = await CategoryTestData.LoadAsync(factory, destination.Id);
		using var client = factory.CreateHttpsClient();
		var path = CategoryTestData.Path(game.Id, source.Id, category.Id);

		interceptor.Enabled = true;
		using var response = operation switch
		{
			"create" => await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, source.Id),
				CategoryApiTests.Body("New")),
			"rename" => await client.PatchAsJsonAsync(path, new { name = "New", order = 20 }),
			_ => await client.PatchAsJsonAsync(path, new { categoryGroupId = destination.Id, name = "New", order = 20 })
		};

		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
		Assert.Contains("name", await response.Content.ReadAsStringAsync());
		Assert.Equal(before, await CategoryTestData.LoadAsync(factory, source.Id));
		Assert.Equal(beforeDestination, await CategoryTestData.LoadAsync(factory, destination.Id));
	}

	[Fact]
	public async Task NoOpNeverCallsSaveChanges()
	{
		var interceptor = new FailAfterSave();
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();

		interceptor.Enabled = true;
		using var response = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, category.Id),
			new { name = " Category ", categoryGroupId = group.Id });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(category.UpdatedAt, (await CategoryApiTests.ReadAsync(response)).UpdatedAt);
	}

	private sealed class FailAfterSave : SaveChangesInterceptor
	{
		internal bool Enabled { get; set; }

		public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
			CancellationToken cancellationToken = default)
		{
			if (Enabled)
				throw new InvalidOperationException("private category write failure");

			return ValueTask.FromResult(result);
		}
	}

	private sealed class ForceNameConflict : SaveChangesInterceptor
	{
		internal bool Enabled { get; set; }

		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
			InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			if (Enabled && eventData.Context is { } context)
			{
				foreach (var entry in context.ChangeTracker.Entries<Category>()
					.Where(entry => entry.State is EntityState.Added or EntityState.Modified))
					Assert.True(entry.Entity.Update(name: "Taken").IsSuccess);
			}

			return ValueTask.FromResult(result);
		}
	}
}
