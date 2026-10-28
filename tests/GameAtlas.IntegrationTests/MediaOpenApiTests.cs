using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public sealed class MediaOpenApiTests
{
	[Fact]
	public async Task NativeDiscriminatorsDescribeMixedItemsAndAssetOnlyResponses()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var schemas = root.GetProperty("components").GetProperty("schemas");
		var items = schemas.GetProperty("MediaItemDto");
		var assets = schemas.GetProperty("MediaAssetDto");

		Assert.Equal("type", items.GetProperty("discriminator").GetProperty("propertyName").GetString());
		Assert.Equal("type", assets.GetProperty("discriminator").GetProperty("propertyName").GetString());
		Assert.Equal(3, items.GetProperty("discriminator").GetProperty("mapping").EnumerateObject().Count());
		Assert.Equal(2, assets.GetProperty("discriminator").GetProperty("mapping").EnumerateObject().Count());
		Assert.False(assets.GetProperty("discriminator").GetProperty("mapping").TryGetProperty("folder", out _));
		var paths = root.GetProperty("paths");
		var listing = schemas.GetProperty("MediaListResult").GetProperty("properties");

		Assert.True(listing.TryGetProperty("items", out _));
		Assert.True(listing.TryGetProperty("total", out _));
		Assert.Contains("null", listing.GetProperty("folder").GetRawText());
		Assert.True(listing.TryGetProperty("ancestors", out _));

		Assert.False(paths.TryGetProperty("/media/files/{id}", out _));
		Assert.True(paths.GetProperty("/media").TryGetProperty("get", out _));
		Assert.True(paths.GetProperty("/media/folders").TryGetProperty("post", out _));
		var upload = paths.GetProperty("/media/assets").GetProperty("post");

		Assert.True(upload.GetProperty("requestBody").GetProperty("content")
			.TryGetProperty("multipart/form-data", out _));
		Assert.Contains("MediaAssetDto", upload.GetProperty("responses").GetProperty("200").GetRawText());
		Assert.True(paths.GetProperty("/media/folders/{id}").TryGetProperty("patch", out _));
		Assert.True(paths.GetProperty("/media/folders/{id}").TryGetProperty("delete", out _));
		Assert.Equal(["patch", "delete"], paths.GetProperty("/media/folders/{id}").EnumerateObject()
			.Select(operation => operation.Name).ToArray());
		var parent = schemas.GetProperty("CreateMediaFolderRequest").GetProperty("properties").GetProperty("parentId");

		Assert.Contains("\"null\"", parent.GetRawText());
	}
}
