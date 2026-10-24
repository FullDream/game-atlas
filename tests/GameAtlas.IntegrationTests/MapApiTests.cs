using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapApiTests
{
	[Theory]
	[InlineData("text/plain")]
	[InlineData("application/json-patch+json")]
	public async Task StylePatchRejectsUnsupportedMedia(string contentType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var response = await client.PatchAsync($"{MapTestData.PathFor(Guid.NewGuid())}/styles/{Guid.NewGuid()}",
			new StringContent("{\"name\":\"Atlas\"}", Encoding.UTF8, contentType));

		Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
	}

	[Fact]
	public async Task StyleRenameConflictsAndUnknownScopesAreAtomic()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var created = await MapTestData.CreateWithStylesAsync(client, game.Id);
		var map = await MapTestData.ReadAsync(created);
		var style = map.Styles.Last();
		using var conflict = await client.PatchAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}",
			new { name = " atlas ", backgroundColor = "#123" });
		using var missing = await client.PatchAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles/{Guid.NewGuid()}",
			new { name = "Changed" });
		using var foreign = await client.PatchAsJsonAsync(
			$"{MapTestData.PathFor(Guid.NewGuid())}/styles/{style.Id}",
			new { name = "Changed" });

		Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(style, (await MapTestData.ReadAsync(read)).Styles.Last());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AdmissionProcessingTilesAndDeletionAreDurable(bool filesFirst)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "map-game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style(fileFirst: filesFirst);
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = Assert.Single(map.Styles);
		var storage = factory.Services.GetRequiredService<IMapStorage>();

		Assert.Equal(HttpStatusCode.OK, created.StatusCode);
		Assert.Equal(23, map.Width);
		Assert.Equal(17, map.Height);
		Assert.Equal([-11.5, -8.5, 11.5, 8.5], map.Extent);
		Assert.Equal([-11.5, 8.5], map.TileOrigin);
		Assert.Equal([1d], map.Resolutions);
		Assert.Null(map.DefaultStyleId);
		Assert.Null(style.TilesUrlTemplate);
		Assert.Equal(MapProcessingStatus.Queued, style.Status);
		Assert.Equal("#FFFFFF", style.BackgroundColor);
		Assert.Equal(MapTestData.ImageBytes(), await File.ReadAllBytesAsync(
			storage.OriginalPath(map.Id, style.Id)));
		using var list = await client.GetAsync("/games");
		var page = await list.Content.ReadFromJsonAsync<JsonElement>();

		Assert.False(page.GetProperty("items")[0].TryGetProperty("mapId", out _));
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var ready = await MapTestData.ReadAsync(read);
		var readyStyle = Assert.Single(ready.Styles);

		Assert.Equal(MapProcessingStatus.Ready, readyStyle.Status);
		Assert.Equal(100, readyStyle.Progress);
		Assert.Equal(style.Id, ready.DefaultStyleId);
		var tileUrl = readyStyle.TilesUrlTemplate!.Replace("{z}", "0").Replace("{x}", "0").Replace("{y}", "0");
		using var tile = await client.GetAsync(tileUrl);

		Assert.Equal(HttpStatusCode.OK, tile.StatusCode);
		Assert.Equal("image/webp", tile.Content.Headers.ContentType!.MediaType);
		using var decoded = NetVips.Image.NewFromBuffer(await tile.Content.ReadAsByteArrayAsync());

		Assert.Equal(512, decoded.Width);
		Assert.Equal(512, decoded.Height);
		using var conditional = new HttpRequestMessage(HttpMethod.Get, tileUrl);

		conditional.Headers.IfNoneMatch.Add(tile.Headers.ETag!);
		using var notModified = await client.SendAsync(conditional);

		Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
		using var last = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}");

		Assert.Equal(HttpStatusCode.NoContent, last.StatusCode);
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Empty(await deleted.Content.ReadAsByteArrayAsync());
		using var missing = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		using var repeated = await client.DeleteAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode);
		var publishedUrl = MapTestData.TilesPath(map.Id, style.Id) + "/0/0/0.webp";
		using var retainedTile = await client.GetAsync(publishedUrl);

		Assert.Equal(HttpStatusCode.OK, retainedTile.StatusCode);
		await processor.CleanupAsync(CancellationToken.None);
		using var removedTile = await client.GetAsync(publishedUrl);

		Assert.Equal(HttpStatusCode.NotFound, removedTile.StatusCode);
		Assert.False(File.Exists(storage.OriginalPath(map.Id, style.Id)));
		await using var scope = factory.Services.CreateAsyncScope();
		var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.SingleAsync();

		DeleteGameHandlerTests.AssertSameGame(game, stored);
	}

	[Theory]
	[InlineData("{}", 422)]
	[InlineData("{\"name\":null}", 422)]
	[InlineData("{\"backgroundColor\":null}", 422)]
	[InlineData("{\"backgroundColor\":\"#ffff\"}", 422)]
	[InlineData("{\"id\":null,\"name\":\"Changed\"}", 400)]
	[InlineData("{\"status\":\"ready\"}", 400)]
	[InlineData("[]", 400)]
	[InlineData("null", 400)]
	[InlineData("{", 400)]
	public async Task StylePatchRejectsInvalidChangesAtomically(string json, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		var style = map.Styles.Single();
		using var client = factory.CreateHttpsClient();
		using var response = await client.PatchAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}",
			new StringContent(json, Encoding.UTF8, "application/merge-patch+json"));

		Assert.Equal(status, (int)response.StatusCode);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var stored = Assert.Single((await MapTestData.ReadAsync(read)).Styles);

		Assert.Equal(style.Name, stored.Name);
		Assert.Equal(style.BackgroundColor, stored.BackgroundColor);
		Assert.Equal(style.UpdatedAt, stored.UpdatedAt);
	}

	[Theory]
	[InlineData("application/json")]
	[InlineData("application/merge-patch+json")]
	public async Task MetadataCanChangeWhileQueuedAndNormalizedNoOpPreservesTimestamp(string contentType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		var style = map.Styles.Single();
		using var client = factory.CreateHttpsClient();
		var url = $"{MapTestData.PathFor(game.Id)}/styles/{style.Id}";
		using var noOp = await client.PatchAsync(url,
			new StringContent("{\"name\":\" Atlas \",\"backgroundColor\":\"#ffffff\"}", Encoding.UTF8, contentType));
		var unchanged = await noOp.Content.ReadFromJsonAsync<MapStyleDto>(MapTestData.JsonOptions);

		Assert.Equal(HttpStatusCode.OK, noOp.StatusCode);
		Assert.Equal(style.UpdatedAt, unchanged!.UpdatedAt);
		using var changed = await client.PatchAsync(url,
			new StringContent("{\"name\":\" Roads \",\"backgroundColor\":\"#abc\"}", Encoding.UTF8, contentType));
		var updated = await changed.Content.ReadFromJsonAsync<MapStyleDto>(MapTestData.JsonOptions);

		Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
		Assert.Equal("Roads", updated!.Name);
		Assert.Equal("#AABBCC", updated.BackgroundColor);
		Assert.Equal(MapProcessingStatus.Queued, updated.Status);
		Assert.Equal(style.Id, updated.Id);
	}
}
