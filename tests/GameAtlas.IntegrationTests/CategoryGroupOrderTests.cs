using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CategoryGroupOrderTests
{
	[Theory]
	[InlineData("null", 400)]
	[InlineData("0", 400)]
	[InlineData("-1", 400)]
	[InlineData("1.5", 400)]
	[InlineData("2147483648", 400)]
	[InlineData("\"1\"", 400)]
	public async Task NumericOrderInputsAreRejectedOnCreateAndUpdate(string value, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);

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
		CategoryGroupTestData.AssertSame(before, await CategoryGroupTestData.LoadAsync(factory, game.Id));
	}

	[Fact]
	public async Task RelativePlacementShiftsPositionsButPreservesSiblingTimestamps()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");

		var first = await CategoryGroupTestData.SeedAsync(factory, game.Id, "First", 1);

		var second = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Second", 2);

		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, second.Id), new { beforeId = first.Id });

		CategoryGroupTestData.AssertSame(HttpStatusCode.OK, response.StatusCode);

		var dto = await response.Content.ReadFromJsonAsync<CategoryGroupDto>();

		CategoryGroupTestData.AssertSame("Second", dto!.Name);
		CategoryGroupTestData.AssertSame(1, await CategoryGroupTestData.OrderAsync(factory, dto.Id));
		Assert.True(dto.UpdatedAt > second.UpdatedAt);
		CategoryGroupTestData.AssertSame(before[0],
			(await CategoryGroupTestData.LoadAsync(factory, game.Id)).Single(item => item.Id == first.Id));

		using var noOp = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, second.Id), new { beforeId = first.Id });

		CategoryGroupTestData.AssertSame(dto, await noOp.Content.ReadFromJsonAsync<CategoryGroupDto>());
		using var both = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, second.Id),
			new { name = " New ", beforeId = (Guid?)null });

		CategoryGroupTestData.AssertSame(HttpStatusCode.OK, both.StatusCode);

		var updated = await both.Content.ReadFromJsonAsync<CategoryGroupDto>();

		CategoryGroupTestData.AssertSame("New", updated!.Name);
		CategoryGroupTestData.AssertSame(2, await CategoryGroupTestData.OrderAsync(factory, updated.Id));
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

		CategoryGroupTestData.AssertSame(third.Id, response![0].Id);
		CategoryGroupTestData.AssertSame(new[] { first.Id, second.Id }.Order(), response.Skip(1).Select(item => item.Id));
	}

	[Fact]
	public async Task DefaultAppendIsServerOwnedAndNumericOrderIsRejected()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");

		await MapTestData.EnsureMapAsync(factory, game.Id);

		using var client = factory.CreateHttpsClient();
		using var first = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "First" });

		CategoryGroupTestData.AssertSame(1, (await CategoryGroupTestData.OrderAsync(factory, (await first.Content.ReadFromJsonAsync<CategoryGroupDto>())!.Id)));
		using var maximum = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id),
			new { name = "Maximum" });

		CategoryGroupTestData.AssertSame(HttpStatusCode.OK, maximum.StatusCode);
		using var exhausted = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "Next" });

		CategoryGroupTestData.AssertSame(HttpStatusCode.OK, exhausted.StatusCode);
		using var duplicate = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id),
			new { name = "Explicit", order = 1 });

		CategoryGroupTestData.AssertSame(HttpStatusCode.BadRequest, duplicate.StatusCode);
		CategoryGroupTestData.AssertSame(3, (await CategoryGroupTestData.LoadAsync(factory, game.Id)).Length);
	}
}
