using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MapStyleLibraryTests
{
	[Fact]
	public async Task ManuallyMissingSourceFailsSafelyAndCanBeRetriedAfterRestoringTheSameBytes()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "missing-source");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = Assert.Single(map.Styles);
		var original = Path.Combine(factory.MediaStoragePath, "assets", style.Source.Id.ToString("N"));
		var bytes = await File.ReadAllBytesAsync(original);
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		File.Delete(original);
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		var failedMap = (await client.GetFromJsonAsync<MapDto>(MapTestData.PathFor(game.Id), MapTestData.JsonOptions))!;
		var failed = Assert.Single(failedMap.Styles);

		Assert.Equal(MapProcessingStatus.Failed, failed.Status);
		Assert.Equal(style.Source, failed.Source);
		Assert.DoesNotContain(factory.MediaStoragePath, failed.Error!.Message);
		await File.WriteAllBytesAsync(original, bytes);
		using var retry = await client.PostAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}/retry", null);

		retry.EnsureSuccessStatusCode();
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		var readyMap = (await client.GetFromJsonAsync<MapDto>(MapTestData.PathFor(game.Id), MapTestData.JsonOptions))!;
		var ready = Assert.Single(readyMap.Styles);

		Assert.Equal(MapProcessingStatus.Ready, ready.Status);
		Assert.Equal(style.Source, ready.Source);
	}

	[Fact]
	public async Task DeletingStyleDoesNotLeaveAnIndependentSourceUseRegistration()
	{
		var images = new MapProcessingTests.ControlledImages { Pause = true };
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMapImageProcessor>(images));

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "active-reader");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = Assert.Single(map.Styles);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		var processing = factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(timeout.Token);

		try
		{
			await images.Started.Task.WaitAsync(timeout.Token);
			using var removed = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}");
			using var protectedDelete = await client.DeleteAsync($"/media/assets/{style.Source.Id}");

			Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
			Assert.Equal(HttpStatusCode.NoContent, protectedDelete.StatusCode);
		}
		finally
		{
			images.Release.TrySetResult();
			await processing;
		}

		using var deleted = await client.DeleteAsync($"/media/assets/{style.Source.Id}");

		Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
	}

	[Theory]
	[InlineData("png")]
	[InlineData("jpeg")]
	[InlineData("webp")]
	[InlineData("avif")]
	public async Task SharedOriginalProcessesWithoutCopiesAndSurvivesMapDeletion(string format)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "shared-source");
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client, format);
		using var created = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = "World" });
		var map = await MapTestData.ReadAsync(created);
		var path = MapTestData.PathFor(game.Id, map.Id);
		using var added = await client.PostAsJsonAsync(path + "/styles",
			new { name = "Atlas", backgroundColor = "#fff", sourceAssetId = image.Id });
		var style = Assert.Single((await MapTestData.ReadAsync(added)).Styles);

		Assert.Equal(image, style.Source);
		Assert.False(Directory.Exists(Path.Combine(factory.MapStoragePath, "content")));
		using var inUse = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.Conflict, inUse.StatusCode);
		Assert.True(await factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(CancellationToken.None));
		var processed = (await client.GetFromJsonAsync<MapDto>(path, MapTestData.JsonOptions))!.Styles.Single();

		Assert.Equal(MapProcessingStatus.Ready, processed.Status);
		using var tile = await client.GetAsync(processed.TilesUrlTemplate!
			.Replace("{z}", "0").Replace("{x}", "0").Replace("{y}", "0"));

		tile.EnsureSuccessStatusCode();
		using var edited = await client.PatchAsJsonAsync($"/media/assets/{image.Id}", new { alt = "Shared alt" });

		edited.EnsureSuccessStatusCode();
		var unchanged = (await client.GetFromJsonAsync<MapDto>(path, MapTestData.JsonOptions))!.Styles.Single();

		Assert.Equal("Shared alt", unchanged.Source.Alt);
		Assert.Equal(processed.UpdatedAt, unchanged.UpdatedAt);
		Assert.Equal(processed.Status, unchanged.Status);
		Assert.Equal(processed.Source.Placeholder, unchanged.Source.Placeholder);
		using var deleted = await client.DeleteAsync(path);

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		await factory.Services.GetRequiredService<MapProcessor>().CleanupAsync(CancellationToken.None);
		using var original = await client.GetAsync(image.Url);

		original.EnsureSuccessStatusCode();
		using var assetDeleted = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.NoContent, assetDeleted.StatusCode);
	}

	[Fact]
	public async Task InvalidSourcesAndMultipartCannotCreateStyles()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "invalid-source");
		var map = await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();

		var svg = await MediaTestData.UploadImageAsync(client, "svg");
		var folder = await MediaFolderTests.CreateAsync(client, "Not a source");
		var path = MapTestData.PathFor(game.Id, map.Id);

		await MediaTestData.EnsureMarkerAssetsAsync(factory);

		foreach (var source in new[] { svg.Id, folder.Id, MediaTestData.VideoId })
		{
			using var rejected = await client.PostAsJsonAsync(path + "/styles",
				new { name = "Atlas", backgroundColor = "#fff", sourceAssetId = source });

			Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
		}

		using var missing = await client.PostAsJsonAsync(path + "/styles",
			new { name = "Atlas", backgroundColor = "#fff", sourceAssetId = Guid.NewGuid() });
		using var multipart = MapTestData.Style();
		using var unsupported = await client.PostAsync(path + "/styles", multipart);

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
		Assert.Empty((await client.GetFromJsonAsync<MapDto>(path, MapTestData.JsonOptions))!.Styles);
	}
}
