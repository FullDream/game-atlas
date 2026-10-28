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
		var items = schemas.GetProperty("MediaItem");
		var assets = schemas.GetProperty("MediaAsset");

		Assert.Equal("type", items.GetProperty("discriminator").GetProperty("propertyName").GetString());
		Assert.Equal("type", assets.GetProperty("discriminator").GetProperty("propertyName").GetString());
		Assert.Equal(3, items.GetProperty("discriminator").GetProperty("mapping").EnumerateObject().Count());
		Assert.Equal(2, assets.GetProperty("discriminator").GetProperty("mapping").EnumerateObject().Count());
		Assert.False(assets.GetProperty("discriminator").GetProperty("mapping").TryGetProperty("folder", out _));
		var paths = root.GetProperty("paths");
		var listing = schemas.GetProperty("PaginatedResultOfMediaItem").GetProperty("properties");

		Assert.True(listing.TryGetProperty("items", out _));
		Assert.True(listing.TryGetProperty("total", out _));
		Assert.False(listing.TryGetProperty("folder", out _));
		Assert.False(listing.TryGetProperty("ancestors", out _));

		Assert.False(paths.TryGetProperty("/media/files/{id}", out _));
		Assert.True(paths.GetProperty("/media").TryGetProperty("get", out _));
		Assert.True(paths.GetProperty("/media/folders").TryGetProperty("post", out _));
		var upload = paths.GetProperty("/media/assets").GetProperty("post");

		Assert.True(upload.GetProperty("requestBody").GetProperty("content")
			.TryGetProperty("multipart/form-data", out _));
		Assert.Contains("MediaAsset", upload.GetProperty("responses").GetProperty("200").GetRawText());
		Assert.True(paths.GetProperty("/media/folders/{id}").TryGetProperty("patch", out _));
		Assert.True(paths.GetProperty("/media/folders/{id}").TryGetProperty("delete", out _));
		Assert.Equal(["get", "patch", "delete"], paths.GetProperty("/media/folders/{id}").EnumerateObject()
			.Select(operation => operation.Name).ToArray());
		var parent = schemas.GetProperty("CreateMediaFolderRequest").GetProperty("properties").GetProperty("parentId");

		Assert.Contains("\"null\"", parent.GetRawText());
	}

	[Fact]
	public async Task CanonicalSchemasPreserveMappingsLiteralTypesNullabilityAndReferences()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var schemas = root.GetProperty("components").GetProperty("schemas");

		Assert.DoesNotContain(schemas.EnumerateObject(),
			schema => schema.Name.Contains("Dto", StringComparison.Ordinal));

		foreach (var (name, value) in new[]
			{ ("MediaFolder", "folder"), ("ImageAsset", "image"), ("VideoAsset", "video") })
		{
			var concrete = schemas.GetProperty(name);
			var reference = $"#/components/schemas/{name}";

			Assert.Equal("object", concrete.GetProperty("type").GetString());
			Assert.False(concrete.TryGetProperty("anyOf", out _));
			Assert.False(concrete.TryGetProperty("discriminator", out _));
			Assert.Contains(concrete.GetProperty("required").EnumerateArray(), field => field.GetString() == "type");
			Assert.Equal(value, Assert.Single(concrete.GetProperty("properties").GetProperty("type")
				.GetProperty("enum").EnumerateArray()).GetString());

			foreach (var unionName in value == "folder" ? new[] { "MediaItem" } : ["MediaItem", "MediaAsset"])
			{
				var union = schemas.GetProperty(unionName);

				Assert.Equal(reference, union.GetProperty("discriminator").GetProperty("mapping")
					.GetProperty(value).GetString());
				Assert.Contains(union.GetProperty("anyOf").EnumerateArray(),
					variant => variant.GetProperty("$ref").GetString() == reference);
				Assert.False(schemas.TryGetProperty(unionName + name, out _));
			}

			Assert.False(schemas.TryGetProperty(name + name, out _));
		}

		var preview = schemas.GetProperty("VideoAsset").GetProperty("properties").GetProperty("preview");
		var alternatives = preview.GetProperty("oneOf").EnumerateArray().ToArray();

		Assert.Equal(2, alternatives.Length);
		Assert.Contains(alternatives, alternative => alternative.TryGetProperty("type", out var type) &&
			type.GetString() == "null");
		Assert.Contains(alternatives, alternative => alternative.TryGetProperty("$ref", out var reference) &&
			reference.GetString() == "#/components/schemas/ImageAsset");
		Assert.False(preview.TryGetProperty("discriminator", out _));
		Assert.False(preview.TryGetProperty("anyOf", out _));
		AssertLocalReferences(root, schemas);
		using var repeated = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

		Assert.Equal(schemas.GetRawText(), repeated.RootElement.GetProperty("components")
			.GetProperty("schemas").GetRawText());
	}

	private static void AssertLocalReferences(JsonElement element, JsonElement schemas)
	{
		if (element.ValueKind == JsonValueKind.Object)
		{
			foreach (var property in element.EnumerateObject())
				AssertLocalReferences(property.Value, schemas);
		}
		else if (element.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in element.EnumerateArray())
				AssertLocalReferences(item, schemas);
		}
		else if (element.ValueKind == JsonValueKind.String && element.GetString() is { } reference &&
			reference.StartsWith("#/components/schemas/", StringComparison.Ordinal))
		{
			Assert.True(schemas.TryGetProperty(reference.Split('/')[^1], out _), $"Unresolved reference: {reference}");
		}
	}
}
