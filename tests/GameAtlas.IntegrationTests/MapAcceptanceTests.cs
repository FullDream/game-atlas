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
			new { name = "Dungeon", width = 512, height = 512 });
		var sibling = await MapTestData.ReadAsync(siblingCreation);

		var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonDocument.Parse(MarkerTestData.MixedMedia).RootElement.Clone());
		using var created = await MapTestData.CreateWithStylesAsync(client, game.Id);
		var map = await MapTestData.ReadAsync(created);
		var atlas = map.Styles.First();
		var roads = map.Styles.Last();
		var processor = factory.Services.GetRequiredService<MapProcessor>();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		using var viewer = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
		using var events = await client.GetAsync($"{MapTestData.PathFor(game.Id)}/events",
			HttpCompletionOption.ResponseHeadersRead, viewer.Token);
		using var reader = new StreamReader(await events.Content.ReadAsStreamAsync(timeout.Token));

		Assert.Equal("snapshot", (await MapEventStreamTests.ReadEventAsync(reader, timeout.Token)).Event);
		Assert.True(await processor.RunNextAsync(timeout.Token));
		Assert.True(await processor.RunNextAsync(timeout.Token));
		var partial = await UntilAsync(reader, data =>
			data.GetProperty("styles")[0].GetProperty("status").GetString() == "failed" &&
			data.GetProperty("styles")[1].GetProperty("status").GetString() == "ready", timeout.Token);

		Assert.Equal(roads.Id, partial.GetProperty("defaultStyleId").GetGuid());
		using var retryFirst = await client.PostAsync(
			$"{MapTestData.PathFor(game.Id)}/styles/{atlas.Id}/retry", null);

		Assert.Equal(HttpStatusCode.OK, retryFirst.StatusCode);
		Assert.True(await processor.RunNextAsync(timeout.Token));
		var firstReady = await UntilAsync(reader, data =>
			data.GetProperty("styles")[0].GetProperty("status").GetString() == "ready", timeout.Token);

		Assert.Equal(atlas.Id, firstReady.GetProperty("defaultStyleId").GetGuid());
		using var patch = await client.PatchAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles/{atlas.Id}",
			new { name = "Atlas new", backgroundColor = "#abc" });
		var updated = (await patch.Content.ReadFromJsonAsync<MapStyleDto>(MapTestData.JsonOptions))!;
		var published = await UntilAsync(reader, data =>
			data.GetProperty("styles")[0].GetProperty("name").GetString() == "Atlas new", timeout.Token);

		Assert.Equal("#AABBCC", published.GetProperty("styles")[0].GetProperty("backgroundColor").GetString());
		var tileUrl = updated.TilesUrlTemplate!.Replace("{z}", "0").Replace("{x}", "0").Replace("{y}", "0");
		using var tile = await client.GetAsync(tileUrl);
		using var pixels = NetVips.Image.NewFromBuffer(await tile.Content.ReadAsByteArrayAsync());

		Assert.Equal(0d, pixels[0, 0][0]);
		viewer.Cancel();
		events.Dispose();
		using var reconnectCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
		using var reconnected = await client.GetAsync($"{MapTestData.PathFor(game.Id)}/events",
			HttpCompletionOption.ResponseHeadersRead, reconnectCancellation.Token);
		using var reconnectedReader = new StreamReader(await reconnected.Content.ReadAsStreamAsync(timeout.Token));
		var snapshot = await MapEventStreamTests.ReadEventAsync(reconnectedReader, timeout.Token);

		Assert.Equal("snapshot", snapshot.Event);
		Assert.Equal("ready", snapshot.Data.GetProperty("styles")[0].GetProperty("status").GetString());
		reconnectCancellation.Cancel();
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
			new { name = "World", width = 11, height = 9 });
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

	private static async Task<JsonElement> UntilAsync(StreamReader reader, Func<JsonElement, bool> predicate,
		CancellationToken cancellationToken)
	{
		while (true)
		{
			var item = await MapEventStreamTests.ReadEventAsync(reader, cancellationToken);

			if (item.Event is "snapshot" or "progress" && predicate(item.Data))
				return item.Data;
		}
	}
}
