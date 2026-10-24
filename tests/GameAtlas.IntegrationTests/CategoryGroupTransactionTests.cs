using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using GameAtlas.Domain.CategoryGroups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GameAtlas.IntegrationTests;

public class CategoryGroupTransactionTests
{
	[Fact]
	public async Task ConcurrentCreatesAllocateDistinctPositionsAndRejectDuplicateNames()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var tasks = Enumerable.Range(0, 3).Select(index => Task.Run(async () =>
		{
			await gate.Task;

			return await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = $"Group {index}" });
		})).ToArray();

		gate.SetResult();
		var responses = await Task.WhenAll(tasks);

		foreach (var response in responses)
		{
			using (response)
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		Assert.Equal(new[] { 1, 2, 3 },
			(await CategoryGroupTestData.LoadAsync(factory, game.Id)).Select(group => group.Order));

		var nameGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var duplicates = new[] { "Ресурсы", " ресурсы " }.Select(name => Task.Run(async () =>
		{
			await nameGate.Task;

			return await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name });
		})).ToArray();

		nameGate.SetResult();
		var duplicateResponses = await Task.WhenAll(duplicates);

		Assert.Equal(1, duplicateResponses.Count(response => response.StatusCode == HttpStatusCode.OK));
		Assert.Equal(1, duplicateResponses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

		foreach (var response in duplicateResponses)
			response.Dispose();
	}

	[Theory]
	[InlineData("create")]
	[InlineData("update")]
	[InlineData("delete")]
	public async Task UnexpectedWriteFailuresReturnSafe500WithoutMutation(string operation)
	{
		var interceptor = new GroupWriteFailure();
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();

		interceptor.Arm(1);
		using var response = operation switch
		{
			"create" => await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "New" }),
			"update" => await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, group.Id),
				new { name = "New", order = 20 }),
			_ => await client.DeleteAsync(CategoryGroupApiTests.Path(game.Id, group.Id))
		};

		await AssertSafe500Async(response);
		Assert.Equal(before, await CategoryGroupTestData.LoadAsync(factory, game.Id));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ConstraintDetectedDuringSavingReturns409AndRollsBack(bool rename)
	{
		var interceptor = new ForceNameConflict();
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");

		await CategoryGroupTestData.SeedAsync(factory, game.Id, "Taken", 1);
		var target = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Target", 2);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();

		interceptor.Enabled = true;
		using var response = rename
			? await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, target.Id),
				new { name = "Available", order = 20 })
			: await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "Available" });

		await CategoryGroupApiTests.AssertProblemAsync(response, HttpStatusCode.Conflict, "name");
		Assert.Equal(before, await CategoryGroupTestData.LoadAsync(factory, game.Id));
	}

	private static async Task AssertSafe500Async(HttpResponseMessage response)
	{
		await CategoryGroupApiTests.AssertProblemAsync(response, HttpStatusCode.InternalServerError);
		var body = await response.Content.ReadAsStringAsync();

		Assert.DoesNotContain("private write failure", body);
		Assert.DoesNotContain("CategoryGroups", body);
		Assert.DoesNotContain("SQLite", body);
		Assert.DoesNotContain("InvalidOperationException", body);
	}

	private sealed class ForceNameConflict : SaveChangesInterceptor
	{
		internal bool Enabled { get; set; }

		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
			DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			if (Enabled && eventData.Context is { } context)
			{
				foreach (var entry in context.ChangeTracker.Entries<CategoryGroup>()
					.Where(entry => entry.State is EntityState.Added or EntityState.Modified))
					Assert.True(entry.Entity.Update("Taken").IsSuccess);
			}

			return ValueTask.FromResult(result);
		}
	}

	private sealed class GroupWriteFailure : DbCommandInterceptor
	{
		private int writeCount;
		private int failAt;

		internal int WriteCount => Volatile.Read(ref writeCount);

		internal void Arm(int statement)
		{
			writeCount = 0;
			failAt = statement;
		}

		private void Observe(DbCommand command)
		{
			var sql = command.CommandText;

			if (failAt == 0 || !sql.Contains("\"CategoryGroups\"", StringComparison.Ordinal) ||
				!(sql.StartsWith("INSERT", StringComparison.Ordinal) || sql.StartsWith("UPDATE", StringComparison.Ordinal) ||
				  sql.StartsWith("DELETE", StringComparison.Ordinal)))
				return;

			if (Interlocked.Increment(ref writeCount) == failAt)
				throw new InvalidOperationException("private write failure: " + sql);
		}

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
			DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			Observe(command);

			return ValueTask.FromResult(result);
		}

		public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
			DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
			CancellationToken cancellationToken = default)
		{
			Observe(command);

			return ValueTask.FromResult(result);
		}
	}
}
