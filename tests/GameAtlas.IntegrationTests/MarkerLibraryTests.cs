using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Media;

namespace GameAtlas.IntegrationTests;

public sealed class MarkerLibraryTests
{
	[Fact]
	public async Task AssetsCanBeSharedAcrossGamesButFoldersCannotBeAttached()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (firstGame, firstGroup) = await CategoryTestData.ParentAsync(factory);
		var (secondGame, secondGroup) = await CategoryTestData.ParentAsync(factory);
		var firstCategory = await CategoryTestData.SeedAsync(factory, firstGroup.Id);
		var secondCategory = await CategoryTestData.SeedAsync(factory, secondGroup.Id);
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client, "svg");
		var media = JsonSerializer.SerializeToElement(new[] { image.Id });
		var first = await MarkerTestData.CreateAsync(client, firstGame.Id, firstCategory.Id, media: media);
		var second = await MarkerTestData.CreateAsync(client, secondGame.Id, secondCategory.Id, media: media);
		var folder = await MediaFolderTests.CreateAsync(client, "Not an asset");
		using var invalid = await client.PatchAsJsonAsync(
			MarkerTestData.Path(secondGame.Id) + $"/{second.GetProperty("id").GetGuid()}",
			new { name = "Must not persist", media = new[] { folder.Id } });

		Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
		using var deleted = await client.DeleteAsync($"/games/{firstGame.Id}");
		using var protectedAsset = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, protectedAsset.StatusCode);
		var retained = await MarkerTestData.LoadAsync(factory, second.GetProperty("id").GetGuid());

		Assert.Equal("Marker", retained.Name);
		Assert.Equal(image.Id, Assert.Single(retained.Media).AssetId);
		using var original = await client.GetAsync(image.Url);

		Assert.Equal(HttpStatusCode.OK, original.StatusCode);
		Assert.NotEqual(first.GetProperty("id").GetGuid(), retained.Id);
	}

	[Theory]
	[InlineData("null", 422)]
	[InlineData("[null]", 422)]
	[InlineData("[\"invalid\"]", 400)]
	[InlineData("[\"00000000-0000-0000-0000-000000000000\"]", 422)]
	[InlineData("[\"019a0000-0000-7000-8000-000000000999\"]", 404)]
	[InlineData("[\"019a0000-0000-7000-8000-000000000999\",\"019a0000-0000-7000-8000-000000000999\"]", 422)]
	public async Task InvalidAssetArraysDoNotPartiallyMutateMarkers(string media, int expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var created = await MarkerTestData.CreateAsync(client, game.Id, category.Id);
		var id = created.GetProperty("id").GetGuid();
		using var body = new StringContent("{\"name\":\"Must not persist\",\"media\":" + media + "}",
			Encoding.UTF8, "application/merge-patch+json");
		using var response = await client.PatchAsync(MarkerTestData.Path(game.Id) + $"/{id}", body);

		Assert.Equal(expected, (int)response.StatusCode);
		var stored = await MarkerTestData.LoadAsync(factory, id);

		Assert.Equal("Marker", stored.Name);
		Assert.Empty(stored.Media);
	}

	[Fact]
	public async Task SharedImageVideoAndPreviewKeepMetadataAndBytesThroughReorderingAndDetachment()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client);
		var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Media", "flower.mp4"));
		using var upload = new MultipartFormDataContent();

		upload.Add(new ByteArrayContent(bytes), "file", "video.mp4");
		upload.Add(new StringContent(image.Id.ToString()), "previewAssetId");
		using var uploaded = await client.PostAsync("/media/assets", upload);
		var video = Assert.IsType<VideoAssetDto>(await uploaded.Content.ReadFromJsonAsync<MediaAssetDto>());
		using var creation = await client.PostAsJsonAsync(MarkerTestData.Path(game.Id), new
		{
			categoryId = category.Id,
			name = "Point",
			position = new { x = 0, y = 0 },
			media = new[] { image.Id, video.Id }
		});
		var marker = (await creation.Content.ReadFromJsonAsync<MarkerDto>())!;

		Assert.Equal(HttpStatusCode.OK, creation.StatusCode);
		Assert.Equal(image, marker.Media.First());
		Assert.Equal(video, marker.Media.Last());
		using var metadata = await client.PatchAsJsonAsync($"/media/assets/{image.Id}", new { alt = "Shared entrance" });

		Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
		var listed = await client.GetFromJsonAsync<MarkerDto[]>(MarkerTestData.Path(game.Id));
		var current = Assert.Single(listed!);

		Assert.Equal(marker.UpdatedAt, current.UpdatedAt);
		Assert.Equal("Shared entrance", Assert.IsType<ImageAssetDto>(current.Media.First()).Alt);
		Assert.Equal("Shared entrance", Assert.IsType<VideoAssetDto>(current.Media.Last()).Preview!.Alt);
		using var reorder = await client.PatchAsJsonAsync(MarkerTestData.Path(game.Id) + $"/{marker.Id}",
			new { media = new[] { video.Id, image.Id } });

		Assert.Equal(HttpStatusCode.OK, reorder.StatusCode);
		var reversed = (await reorder.Content.ReadFromJsonAsync<MarkerDto>())!;

		Assert.Equal(new[] { video.Id, image.Id }, reversed.Media.Select(asset => asset.Id));
		using var noOp = await client.PatchAsJsonAsync(MarkerTestData.Path(game.Id) + $"/{marker.Id}",
			new { media = new[] { video.Id, image.Id } });

		Assert.Equal(reversed.UpdatedAt, (await noOp.Content.ReadFromJsonAsync<MarkerDto>())!.UpdatedAt);
		using var protectedVideo = await client.DeleteAsync($"/media/assets/{video.Id}");

		Assert.Equal(HttpStatusCode.Conflict, protectedVideo.StatusCode);
		using var deleted = await client.DeleteAsync(MarkerTestData.Path(game.Id) + $"/{marker.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		using var original = await client.GetAsync(video.Url);

		Assert.Equal(bytes, await original.Content.ReadAsByteArrayAsync());
		using var previewProtected = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.Conflict, previewProtected.StatusCode);
		using var detachedPreview = await client.PatchAsJsonAsync($"/media/assets/{video.Id}",
			new { previewAssetId = (Guid?)null });

		Assert.Equal(HttpStatusCode.OK, detachedPreview.StatusCode);
		using var unusedImage = await client.DeleteAsync($"/media/assets/{image.Id}");
		using var unusedVideo = await client.DeleteAsync($"/media/assets/{video.Id}");

		Assert.Equal(HttpStatusCode.NoContent, unusedImage.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, unusedVideo.StatusCode);
	}
}
