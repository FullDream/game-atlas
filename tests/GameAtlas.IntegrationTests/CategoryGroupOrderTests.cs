using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.CategoryGroups;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CategoryGroupOrderTests
{
	[Theory]
	[InlineData("null", 422)]
	[InlineData("0", 422)]
	[InlineData("-1", 422)]
	[InlineData("1.5", 400)]
	[InlineData("2147483648", 400)]
	[InlineData("\"1\"", 400)]
	public async Task InvalidOrderOnCreateAndUpdateDoesNotPersist(string value, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		var body = "{\"name\":\"New\",\"order\":" + value + "}";
		using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
		using var creation = await client.PostAsync(CategoryGroupApiTests.Path(game.Id), content);
		using var update = await CategoryGroupApiTests.PatchAsync(client,
			CategoryGroupApiTests.Path(game.Id, group.Id), body);

		await CategoryGroupApiTests.AssertProblemAsync(creation, (HttpStatusCode)status);
		await CategoryGroupApiTests.AssertProblemAsync(update, (HttpStatusCode)status);
		Assert.Equal(before, await CategoryGroupTestData.LoadAsync(factory, game.Id));
	}

	[Fact]
	public async Task UpdatesOrderOrBothFieldsWithoutShiftingOtherGroups()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var first = await CategoryGroupTestData.SeedAsync(factory, game.Id, "First", 1);
		var second = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Second", 2);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, second.Id), new { order = 1 });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var dto = await response.Content.ReadFromJsonAsync<CategoryGroupDto>();

		Assert.Equal("Second", dto!.Name);
		Assert.Equal(1, dto.Order);
		Assert.True(dto.UpdatedAt > second.UpdatedAt);
		Assert.Equal(before[0],
			(await CategoryGroupTestData.LoadAsync(factory, game.Id)).Single(item => item.Id == first.Id));

		using var noOp = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, second.Id), new { order = 1 });

		Assert.Equal(dto, await noOp.Content.ReadFromJsonAsync<CategoryGroupDto>());
		using var both = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, second.Id),
			new { name = " New ", order = 10 });

		Assert.Equal(HttpStatusCode.OK, both.StatusCode);
		var updated = await both.Content.ReadFromJsonAsync<CategoryGroupDto>();

		Assert.Equal("New", updated!.Name);
		Assert.Equal(10, updated.Order);
	}

	[Fact]
	public async Task ListsDuplicateOrdersByCreatedAtThenId()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var first = await CategoryGroupTestData.SeedAsync(factory, game.Id, "First", 10);
		var second = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Second", 10);
		var third = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Third", 10);
		var instant = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var groups = await context.CategoryGroups.AsTracking().ToArrayAsync();

			foreach (var group in groups)
				context.Entry(group).Property(item => item.CreatedAt).CurrentValue =
					group.Id == third.Id ? instant.AddTicks(-1) : instant;

			await context.SaveChangesAsync();
		}

		using var client = factory.CreateHttpsClient();
		var response = await client.GetFromJsonAsync<CategoryGroupDto[]>(CategoryGroupApiTests.Path(game.Id));

		Assert.Equal(third.Id, response![0].Id);
		Assert.Equal(new[] { first.Id, second.Id }.Order(), response.Skip(1).Select(item => item.Id));
	}

	[Fact]
	public async Task DefaultAppendStartsAtOneAndRequiresExplicitOrderWhenRangeExhausted()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var first = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "First" });

		Assert.Equal(1, (await first.Content.ReadFromJsonAsync<CategoryGroupDto>())!.Order);
		using var maximum = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id),
			new { name = "Maximum", order = int.MaxValue });

		Assert.Equal(HttpStatusCode.OK, maximum.StatusCode);
		using var exhausted = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "Next" });

		await CategoryGroupApiTests.AssertProblemAsync(exhausted, HttpStatusCode.UnprocessableEntity, "order");
		using var duplicate = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id),
			new { name = "Explicit", order = 1 });

		Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
		Assert.Equal(3, (await CategoryGroupTestData.LoadAsync(factory, game.Id)).Length);
	}
}
