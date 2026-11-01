using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Infrastructure.Maps.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapAcceptanceTests
{
	[Fact]
	public async Task CompleteWorkflowKeepsContentUntilMapDeletionAndPreservesSiblingMaps()
	{
		var images = new MapProcessingTests.ControlledImages { FailuresRemaining = 1 };
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMapImageProcessor>(images));

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		using var siblingCreation = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = "Dungeon" });
		var sibling = await MapTestData.ReadAsync(siblingCreation);

		var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonDocument.Parse(MarkerTestData.MixedMedia).RootElement.Clone());
		using var created = await MapTestData.CreateWithStylesAsync(client, game.Id);
		var map = await MapTestData.ReadAsync(created);
		var atlas = map.Styles.First();
		var roads = map.Styles.Last();
		var processor = factory.Services.GetRequiredService<MapProcessor>();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		await using var reader = new MapHubViewer(factory);

		await reader.StartAsync(map.Id, timeout.Token);
		Assert.True(await processor.RunNextAsync(timeout.Token));
		Assert.True(await processor.RunNextAsync(timeout.Token));
		await reader.UntilStyleAsync(data => data.GetProperty("id").GetGuid() == atlas.Id &&
			data.GetProperty("status").GetString() == "failed", timeout.Token);
		await reader.UntilStyleAsync(data => data.GetProperty("id").GetGuid() == roads.Id &&
			data.GetProperty("status").GetString() == "ready", timeout.Token);

		await processor.CleanupAsync(timeout.Token);
		using var retryFirst = await client.PostAsync(
			$"{MapTestData.PathFor(game.Id)}/styles/{atlas.Id}/retry", null);

		Assert.Equal(HttpStatusCode.OK, retryFirst.StatusCode);
		await using var retryViewer = new MapHubViewer(factory);

		await retryViewer.StartAsync(map.Id, timeout.Token);
		Assert.True(await processor.RunNextAsync(timeout.Token));
		await retryViewer.UntilStyleAsync(data => data.GetProperty("id").GetGuid() == atlas.Id &&
			data.GetProperty("status").GetString() == "ready", timeout.Token);
		using var readyRead = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(roads.Id, (await MapTestData.ReadAsync(readyRead)).DefaultStyleId);
		using var patch = await client.PatchAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles/{atlas.Id}",
			new { name = "Atlas new", backgroundColor = "#abc" });
		var updated = (await patch.Content.ReadFromJsonAsync<MapStyleDto>(MapTestData.JsonOptions))!;
		Assert.Equal("#AABBCC", updated.BackgroundColor);
		var tileUrl = updated.TilesUrlTemplate!.Replace("{z}", "0").Replace("{x}", "0").Replace("{y}", "0");
		using var tile = await client.GetAsync(tileUrl);
		using var pixels = NetVips.Image.NewFromBuffer(await tile.Content.ReadAsByteArrayAsync());

		Assert.Equal(0d, pixels[0, 0][0]);
		using var deleteDefault = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{atlas.Id}");
		using var readRemaining = await client.GetAsync(MapTestData.PathFor(game.Id));
		var remaining = await MapTestData.ReadAsync(readRemaining);

		Assert.Equal(HttpStatusCode.NoContent, deleteDefault.StatusCode);
		Assert.Equal(roads.Id, remaining.DefaultStyleId);
		using var final = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{roads.Id}");

		Assert.Equal(HttpStatusCode.NoContent, final.StatusCode);
		using var contentBeforeMapDeletion = await client.GetAsync(MarkerTestData.Path(game.Id));
		var retainedMarkers = await contentBeforeMapDeletion.Content.ReadFromJsonAsync<JsonElement>();

		Assert.Equal(marker.GetRawText(), Assert.Single(retainedMarkers.EnumerateArray()).GetRawText());
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id));
		using var recreated = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = "World" });
		var next = await MapTestData.ReadAsync(recreated);
		using var markerRead = await client.GetAsync(MapTestData.PathFor(game.Id, next.Id) + "/markers");
		var markers = await markerRead.Content.ReadFromJsonAsync<JsonElement>();
		using var categoryRead = await client.GetAsync(CategoryTestData.Path(game.Id, group.Id));
		var categories = await categoryRead.Content.ReadFromJsonAsync<JsonElement>();

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.NotEqual(map.Id, next.Id);
		Assert.Equal(0, markers.GetArrayLength());
		Assert.Equal(HttpStatusCode.NotFound, categoryRead.StatusCode);
		using var siblingRead = await client.GetAsync(MapTestData.PathFor(game.Id, sibling.Id));
		var surviving = await MapTestData.ReadAsync(siblingRead);

		Assert.Equal(sibling.Id, surviving.Id);
		Assert.Equal(sibling.CreatedAt, surviving.CreatedAt);
		Assert.Equal(sibling.UpdatedAt, surviving.UpdatedAt);
		Assert.Equal(sibling.Extent, surviving.Extent);
		Assert.Empty(surviving.Styles);
		await processor.CleanupAsync(timeout.Token);
	}

}
