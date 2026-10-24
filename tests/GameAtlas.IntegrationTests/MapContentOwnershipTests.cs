using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Categories;
using GameAtlas.Application.CategoryGroups;
using GameAtlas.Application.Maps;
using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapContentOwnershipTests
{
	[Fact]
	public async Task SiblingMapsOwnIndependentNamesOrdersCountsTransfersAndCascadesWithoutStyles()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		var first = await CreateMapAsync(client, game.Id, "World");
		var second = await CreateMapAsync(client, game.Id, "Dungeon");
		var firstPath = MapTestData.PathFor(game.Id, first.Id);
		var secondPath = MapTestData.PathFor(game.Id, second.Id);
		using var groupCreation = await client.PostAsJsonAsync(firstPath + "/category-groups", new { name = "Resources" });
		using var otherCreation = await client.PostAsJsonAsync(secondPath + "/category-groups", new { name = "Resources" });
		var group = (await groupCreation.Content.ReadFromJsonAsync<CategoryGroupDto>())!;
		var otherGroup = (await otherCreation.Content.ReadFromJsonAsync<CategoryGroupDto>())!;

		Assert.Equal(HttpStatusCode.OK, groupCreation.StatusCode);
		Assert.Equal(HttpStatusCode.OK, otherCreation.StatusCode);
		Assert.Equal(first.Id, group.MapId);
		Assert.Equal(second.Id, otherGroup.MapId);
		Assert.Equal(1, group.Order);
		Assert.Equal(1, otherGroup.Order);
		var categoryPath = firstPath + $"/category-groups/{group.Id}/categories";
		var otherCategoryPath = secondPath + $"/category-groups/{otherGroup.Id}/categories";
		using var categoryCreation = await client.PostAsJsonAsync(categoryPath,
			new { name = "Chest", iconAssetId = MediaTestData.IconId, color = "#fff" });
		using var otherCategoryCreation = await client.PostAsJsonAsync(otherCategoryPath,
			new { name = "Chest", iconAssetId = MediaTestData.IconId, color = "#fff" });
		var category = (await categoryCreation.Content.ReadFromJsonAsync<CategoryDto>())!;
		var otherCategory = (await otherCategoryCreation.Content.ReadFromJsonAsync<CategoryDto>())!;
		using var markerCreation = await client.PostAsJsonAsync(firstPath + "/markers",
			new { categoryId = category.Id, name = "Point", position = new { x = -2.5, y = 100000 } });
		var marker = await markerCreation.Content.ReadFromJsonAsync<JsonElement>();
		var markerId = marker.GetProperty("id").GetGuid();
		using var categoryTransfer = await client.PatchAsJsonAsync(categoryPath + $"/{category.Id}",
			new { categoryGroupId = otherGroup.Id, name = "Changed" });
		using var markerTransfer = await client.PatchAsJsonAsync(firstPath + $"/markers/{markerId}",
			new { categoryId = otherCategory.Id, name = "Changed" });
		using var wrongMarker = await client.DeleteAsync(secondPath + $"/markers/{markerId}");
		using var wrongGroup = await client.GetAsync(firstPath + $"/category-groups/{otherGroup.Id}/categories");

		Assert.Equal(HttpStatusCode.NotFound, categoryTransfer.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, markerTransfer.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, wrongMarker.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, wrongGroup.StatusCode);
		using var counts = await client.GetAsync(categoryPath);
		var beforeDelete = (await counts.Content.ReadFromJsonAsync<CategoryDto[]>())!;

		Assert.Equal(1, Assert.Single(beforeDelete).MarkersCount);
		Assert.Equal("Chest", beforeDelete[0].Name);
		using var removed = await client.DeleteAsync(firstPath);
		using var surviving = await client.GetAsync(otherCategoryPath);
		var remaining = (await surviving.Content.ReadFromJsonAsync<CategoryDto[]>())!;

		Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
		Assert.Equal(otherCategory.Id, Assert.Single(remaining).Id);
		Assert.Equal(0, remaining[0].MarkersCount);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.False(await context.Markers.AnyAsync(item => item.Id == markerId));
		Assert.False(await context.CategoryGroups.AnyAsync(item => item.Id == group.Id));
		Assert.True(await context.Maps.AnyAsync(item => item.Id == second.Id));
		DeleteGameHandlerTests.AssertSameGame(game, await context.Games.SingleAsync());
	}

	[Fact]
	public async Task GameDeletionSchedulesEveryMapAndPreservesOtherGameOriginals()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var other = await UpdateGameHandlerTests.SeedAsync(factory, "other");
		using var client = factory.CreateHttpsClient();
		var first = await CreateMapAsync(client, game.Id, "World");
		var second = await CreateMapAsync(client, game.Id, "Dungeon");
		var unrelated = await CreateMapAsync(client, other.Id, "World");

		foreach (var map in new[] { first, second, unrelated })
		{
			using var upload = MapTestData.Style();
			using var added = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(map.GameId, map.Id), upload);

			Assert.Equal(HttpStatusCode.OK, added.StatusCode);
		}

		using var deleted = await client.DeleteAsync($"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var intents = await context.Set<MapCleanupIntent>().Where(intent => intent.MapId != null &&
				intent.StyleId == null).Select(intent => intent.MapId).ToArrayAsync();

			Assert.Contains(first.Id, intents);
			Assert.Contains(second.Id, intents);
			Assert.Equal(unrelated.Id, (await context.Maps.SingleAsync()).Id);
			Assert.Single(await context.MapStyles.ToArrayAsync());
		}

		await factory.Services.GetRequiredService<MapProcessor>().CleanupAsync(CancellationToken.None);
		Assert.Equal(3, Directory.GetFiles(Path.Combine(factory.MediaStoragePath, "assets")).Length);
		Assert.False(Directory.Exists(Path.Combine(factory.MapStoragePath, "content")));
	}

	private static async Task<MapDto> CreateMapAsync(HttpClient client, Guid gameId, string name)
	{
		using var response = await client.PostAsJsonAsync(MapTestData.CollectionFor(gameId),
			new { name, width = 23, height = 17 });

		return await MapTestData.ReadAsync(response);
	}
}
