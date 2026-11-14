using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Categories;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Application.Markers;
using GameAtlas.Domain;

namespace GameAtlas.IntegrationTests;

public class ContentPublicationStatusTests
{
	[Fact]
	public async Task CreateAndPatchStatusWorkIndependentlyAcrossMapsGroupsCategoriesAndMarkers()
	{
		await using var factory = new GameApiFactory("Development");
		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "pub-status-game");
		using var client = factory.CreateHttpsClient();

		// 1. Map defaults to draft on POST, can be created as published, and rejects null status with 422
		using var mapDefaultRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps", new { name = "Draft Map", slug = "draft-map" });
		Assert.Equal(HttpStatusCode.OK, mapDefaultRes.StatusCode);
		var draftMap = await MapTestData.ReadAsync(mapDefaultRes);
		Assert.Equal(PublicationStatus.Draft, draftMap.Status);

		using var mapPubRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps", new { name = "Published Map", slug = "published-map", status = "published" });
		Assert.Equal(HttpStatusCode.OK, mapPubRes.StatusCode);
		var pubMap = await MapTestData.ReadAsync(mapPubRes);
		Assert.Equal(PublicationStatus.Published, pubMap.Status);

		using var mapNullStatus = await client.PostAsync($"/games/{game.Id}/maps",
			new StringContent("""{"name":"Null Status","slug":"null-status","status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, mapNullStatus.StatusCode);

		// 2. Game DTO includes map status in summaries
		using var gameRes = await client.GetAsync($"/games/{game.Id}");
		var gameJson = await gameRes.Content.ReadFromJsonAsync<JsonElement>();
		var gameMaps = gameJson.GetProperty("maps").EnumerateArray().ToArray();
		Assert.Equal(2, gameMaps.Length);
		Assert.Equal("draft", gameMaps.Single(m => m.GetProperty("id").GetGuid() == draftMap.Id).GetProperty("status").GetString());
		Assert.Equal("published", gameMaps.Single(m => m.GetProperty("id").GetGuid() == pubMap.Id).GetProperty("status").GetString());

		// 3. CategoryGroup defaults to draft on POST, accepts status, and rejects null with 422
		using var groupDefaultRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{draftMap.Id}/category-groups", new { name = "Locations" });
		Assert.Equal(HttpStatusCode.OK, groupDefaultRes.StatusCode);
		var group = (await groupDefaultRes.Content.ReadFromJsonAsync<CategoryGroupDto>(MapTestData.JsonOptions))!;
		Assert.Equal(PublicationStatus.Draft, group.Status);

		using var groupNullStatus = await client.PostAsync($"/games/{game.Id}/maps/{draftMap.Id}/category-groups",
			new StringContent("""{"name":"Bad Group","status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, groupNullStatus.StatusCode);

		// 4. Category defaults to draft on POST, accepts status, and rejects null with 422
		using var catDefaultRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{draftMap.Id}/category-groups/{group.Id}/categories", new { name = "Chests" });
		Assert.Equal(HttpStatusCode.OK, catDefaultRes.StatusCode);
		var category = (await catDefaultRes.Content.ReadFromJsonAsync<CategoryDto>(MapTestData.JsonOptions))!;
		Assert.Equal(PublicationStatus.Draft, category.Status);

		using var catNullStatus = await client.PostAsync($"/games/{game.Id}/maps/{draftMap.Id}/category-groups/{group.Id}/categories",
			new StringContent("""{"name":"Bad Category","status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, catNullStatus.StatusCode);

		// 5. Marker defaults to draft on POST, accepts status, and rejects null with 422
		using var markerDefaultRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{draftMap.Id}/markers", new
		{
			categoryId = category.Id,
			name = "Hidden Chest",
			position = new { x = 10, y = 20 }
		});
		Assert.Equal(HttpStatusCode.OK, markerDefaultRes.StatusCode);
		var marker = (await markerDefaultRes.Content.ReadFromJsonAsync<MarkerDto>(MapTestData.JsonOptions))!;
		Assert.Equal(PublicationStatus.Draft, marker.Status);

		using var markerNullStatus = await client.PostAsync($"/games/{game.Id}/maps/{draftMap.Id}/markers",
			new StringContent($$$"""{"categoryId":"{{{category.Id}}}","name":"Bad Marker","position":{"x":0,"y":0},"status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, markerNullStatus.StatusCode);

		// 6. Individual PATCH updates only the target entity without cascading to children
		using var patchMapRes = await client.PatchAsJsonAsync($"/games/{game.Id}/maps/{draftMap.Id}", new { status = "published" });
		Assert.Equal(HttpStatusCode.OK, patchMapRes.StatusCode);
		Assert.Equal(PublicationStatus.Published, (await MapTestData.ReadAsync(patchMapRes)).Status);

		var treeAfterMapPatch = (await client.GetFromJsonAsync<CategoryGroupDto[]>($"/games/{game.Id}/maps/{draftMap.Id}/category-groups", MapTestData.JsonOptions))!;
		Assert.Equal(PublicationStatus.Draft, treeAfterMapPatch.Single().Status);
		Assert.Equal(PublicationStatus.Draft, treeAfterMapPatch.Single().Categories.Single().Status);

		var markersAfterMapPatch = (await client.GetFromJsonAsync<MarkerDto[]>($"/games/{game.Id}/maps/{draftMap.Id}/markers", MapTestData.JsonOptions))!;
		Assert.Equal(PublicationStatus.Draft, markersAfterMapPatch.Single().Status);

		// 7. PATCH null status returns 422 on all 4 entities
		using var patchMapNull = await client.PatchAsync($"/games/{game.Id}/maps/{draftMap.Id}",
			new StringContent("""{"status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, patchMapNull.StatusCode);

		using var patchGroupNull = await client.PatchAsync($"/games/{game.Id}/maps/{draftMap.Id}/category-groups/{group.Id}",
			new StringContent("""{"status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, patchGroupNull.StatusCode);

		using var patchCatNull = await client.PatchAsync($"/games/{game.Id}/maps/{draftMap.Id}/category-groups/{group.Id}/categories/{category.Id}",
			new StringContent("""{"status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, patchCatNull.StatusCode);

		using var patchMarkerNull = await client.PatchAsync($"/games/{game.Id}/maps/{draftMap.Id}/markers/{marker.Id}",
			new StringContent("""{"status":null}""", Encoding.UTF8, "application/json"));
		Assert.Equal(HttpStatusCode.UnprocessableEntity, patchMarkerNull.StatusCode);
	}

	[Fact]
	public async Task PublishMapTransitionsDraftMapAndAllDraftDescendantsAndIsIdempotent()
	{
		await using var factory = new GameApiFactory("Development");
		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "bulk-publish-game");
		using var client = factory.CreateHttpsClient();

		using var mapRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps", new { name = "World", slug = "world", status = "draft" });
		var map = await MapTestData.ReadAsync(mapRes);

		using var draftGroupRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{map.Id}/category-groups", new { name = "Draft Group", status = "draft" });
		var draftGroup = (await draftGroupRes.Content.ReadFromJsonAsync<CategoryGroupDto>(MapTestData.JsonOptions))!;

		using var pubGroupRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{map.Id}/category-groups", new { name = "Published Group", status = "published" });
		var pubGroup = (await pubGroupRes.Content.ReadFromJsonAsync<CategoryGroupDto>(MapTestData.JsonOptions))!;

		using var draftCatRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{map.Id}/category-groups/{pubGroup.Id}/categories", new { name = "Draft Category", status = "draft" });
		var draftCat = (await draftCatRes.Content.ReadFromJsonAsync<CategoryDto>(MapTestData.JsonOptions))!;

		using var pubCatRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{map.Id}/category-groups/{draftGroup.Id}/categories", new { name = "Published Category", status = "published" });
		var pubCat = (await pubCatRes.Content.ReadFromJsonAsync<CategoryDto>(MapTestData.JsonOptions))!;

		using var draftMarkerRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{map.Id}/markers", new
		{
			categoryId = pubCat.Id,
			name = "Draft Marker",
			position = new { x = 1, y = 2 },
			status = "draft"
		});
		var draftMarker = (await draftMarkerRes.Content.ReadFromJsonAsync<MarkerDto>(MapTestData.JsonOptions))!;

		using var pubMarkerRes = await client.PostAsJsonAsync($"/games/{game.Id}/maps/{map.Id}/markers", new
		{
			categoryId = draftCat.Id,
			name = "Published Marker",
			position = new { x = 3, y = 4 },
			status = "published"
		});
		var pubMarker = (await pubMarkerRes.Content.ReadFromJsonAsync<MarkerDto>(MapTestData.JsonOptions))!;

		// Bulk publish the map
		using var publishRes = await client.PostAsync($"/games/{game.Id}/maps/{map.Id}/publish", null);
		Assert.Equal(HttpStatusCode.OK, publishRes.StatusCode);
		var publishedMap = await MapTestData.ReadAsync(publishRes);
		Assert.Equal(PublicationStatus.Published, publishedMap.Status);
		Assert.True(publishedMap.UpdatedAt >= map.UpdatedAt);

		var groups = (await client.GetFromJsonAsync<CategoryGroupDto[]>($"/games/{game.Id}/maps/{map.Id}/category-groups", MapTestData.JsonOptions))!;
		Assert.All(groups, g => Assert.Equal(PublicationStatus.Published, g.Status));
		Assert.Equal(pubGroup.UpdatedAt, groups.Single(g => g.Id == pubGroup.Id).UpdatedAt);

		var categories = groups.SelectMany(g => g.Categories).ToArray();
		Assert.All(categories, c => Assert.Equal(PublicationStatus.Published, c.Status));
		Assert.Equal(pubCat.UpdatedAt, categories.Single(c => c.Id == pubCat.Id).UpdatedAt);

		var markers = (await client.GetFromJsonAsync<MarkerDto[]>($"/games/{game.Id}/maps/{map.Id}/markers", MapTestData.JsonOptions))!;
		Assert.All(markers, m => Assert.Equal(PublicationStatus.Published, m.Status));
		Assert.Equal(pubMarker.UpdatedAt, markers.Single(m => m.Id == pubMarker.Id).UpdatedAt);

		// Calling publish again when all are published is a no-op that preserves timestamps
		using var secondPublishRes = await client.PostAsync($"/games/{game.Id}/maps/{map.Id}/publish", null);
		Assert.Equal(HttpStatusCode.OK, secondPublishRes.StatusCode);
		var secondMap = await MapTestData.ReadAsync(secondPublishRes);
		Assert.Equal(publishedMap.UpdatedAt, secondMap.UpdatedAt);

		// Foreign or missing map returns 404
		using var missingPublish = await client.PostAsync($"/games/{game.Id}/maps/{Guid.CreateVersion7()}/publish", null);
		Assert.Equal(HttpStatusCode.NotFound, missingPublish.StatusCode);

		using var foreignPublish = await client.PostAsync($"/games/{Guid.CreateVersion7()}/maps/{map.Id}/publish", null);
		Assert.Equal(HttpStatusCode.NotFound, foreignPublish.StatusCode);
	}
}
