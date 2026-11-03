using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Categories;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Media;
using GameAtlas.Infrastructure.Maps.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MediaAcceptanceTests
{
	[Fact]
	public async Task SharedImageAndPreviewPropagateAcrossConsumersAndOutliveTheirGames()
	{
		var images = new MapProcessingTests.ControlledImages { FailuresRemaining = 1 };
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMapImageProcessor>(images));

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();

		var image = await MediaTestData.UploadImageAsync(client);

		var unrelated = await MediaTestData.UploadImageAsync(client, "svg");
		using var upload = new MultipartFormDataContent();

		upload.Add(new ByteArrayContent(await File.ReadAllBytesAsync(
			Path.Combine(AppContext.BaseDirectory, "Fixtures", "Media", "flower.mp4"))), "file", "video.mp4");

		upload.Add(new StringContent(image.Id.ToString()), "previewAssetId");
		using var uploaded = await client.PostAsync("/media/assets", upload);

		var video = Assert.IsType<VideoAssetDto>(await uploaded.Content.ReadFromJsonAsync<MediaAssetDto>());

		var games = new List<Guid>();

		var paths = new List<(string Map, string Category, Guid Marker, Guid Style)>();

		for (var index = 0; index < 2; index++)
		{
			var game = await UpdateGameHandlerTests.SeedAsync(factory, $"shared-{index}");

			var map = await MapTestData.EnsureMapAsync(factory, game.Id);

			var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Resources", 1);

			var categoryPath = CategoryTestData.Path(game.Id, group.Id);
			using var createdCategory = await client.PostAsJsonAsync(categoryPath,
				new { name = "Category", iconAssetId = image.Id, color = "#fff" });

			var category = (await createdCategory.Content.ReadFromJsonAsync<CategoryDto>())!;

			var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
				media: JsonSerializer.SerializeToElement(new[] { image.Id, video.Id }));

			var mapPath = MapTestData.PathFor(game.Id, map.Id);
			using var createdStyle = await client.PostAsJsonAsync(mapPath + "/styles",
				new { name = "Atlas", backgroundColor = "#fff", sourceAssetId = image.Id });

			var style = Assert.Single((await MapTestData.ReadAsync(createdStyle)).Styles);

			games.Add(game.Id);
			paths.Add((mapPath, categoryPath, marker.GetProperty("id").GetGuid(), style.Id));
		}

		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var retry = await client.PostAsync(paths[0].Map + $"/styles/{paths[0].Style}/retry", null);

		retry.EnsureSuccessStatusCode();
		await processor.CleanupAsync(CancellationToken.None);
		Assert.True(await processor.RunNextAsync(CancellationToken.None));

		var folder = await MediaFolderTests.CreateAsync(client, "Shared originals");
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		using var edited = await client.PatchAsJsonAsync($"/media/assets/{image.Id}",
			new { name = "Shared world", alt = "Shared alt", folderId = folder.Id });

		edited.EnsureSuccessStatusCode();
		for (var index = 0; index < paths.Count; index++)
		{
			var categories = (await CategoryTestData.ReadTreeCategoriesAsync(client, paths[index].Category))!;

			var markers = (await client.GetFromJsonAsync<MarkerDto[]>(paths[index].Map + "/markers", MapTestData.JsonOptions))!;

			var map = (await client.GetFromJsonAsync<MapDto>(paths[index].Map, MapTestData.JsonOptions))!;

			var icon = Assert.IsType<ImageAssetDto>(Assert.Single(categories).Icon);

			var media = Assert.Single(markers).Media.ToArray();

			Assert.Equal("Shared alt", icon.Alt);
			Assert.Equal(icon, Assert.IsType<ImageAssetDto>(media[0]));
			Assert.Equal(icon, Assert.IsType<VideoAssetDto>(media[1]).Preview);
			Assert.Equal(icon, Assert.Single(map.Styles).Source);
			Assert.Equal(image.Placeholder, icon.Placeholder);
		}

		timeout.Cancel();
		using var protectedDelete = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.Conflict, protectedDelete.StatusCode);

		foreach (var game in games)
		{
			using var deleted = await client.DeleteAsync($"/games/{game}");

			Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		}

		await processor.CleanupAsync(CancellationToken.None);
		using var retained = await client.GetAsync(image.Url);
		using var previewProtected = await client.DeleteAsync($"/media/assets/{image.Id}");

		retained.EnsureSuccessStatusCode();
		Assert.Equal(HttpStatusCode.Conflict, previewProtected.StatusCode);
		using var deletedVideo = await client.DeleteAsync($"/media/assets/{video.Id}");
		using var deletedImage = await client.DeleteAsync($"/media/assets/{image.Id}");
		using var unrelatedRead = await client.GetAsync(unrelated.Url);

		Assert.Equal(HttpStatusCode.NoContent, deletedVideo.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, deletedImage.StatusCode);
		unrelatedRead.EnsureSuccessStatusCode();
	}
}
