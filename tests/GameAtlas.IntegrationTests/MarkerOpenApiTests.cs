using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class MarkerOpenApiTests
{
	private const string CollectionPath = "/games/{gameId}/maps/{mapId}/markers";

	[Fact]
	public async Task DocumentsOnlyFourOperationsCompleteDtosAndProblemDetails()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var collection = root.GetProperty("paths").GetProperty(CollectionPath);
		var item = root.GetProperty("paths").GetProperty(CollectionPath + "/{id}");

		Assert.Equal(new[] { "get", "post" }, collection.EnumerateObject().Select(property => property.Name));
		Assert.Equal(new[] { "patch", "delete" }, item.EnumerateObject().Select(property => property.Name));

		foreach (var (operation, name) in new[]
		{
			(collection.GetProperty("get"), "ListMarkers"), (collection.GetProperty("post"), "CreateMarker"),
			(item.GetProperty("patch"), "UpdateMarker"), (item.GetProperty("delete"), "DeleteMarker")
		})
		{
			Assert.Equal(name, operation.GetProperty("operationId").GetString());
			Assert.All(operation.GetProperty("parameters").EnumerateArray(),
				parameter => Assert.Equal("path", parameter.GetProperty("in").GetString()));

			foreach (var error in new[] { "400", "404", "500" })
				Assert.True(operation.GetProperty("responses").GetProperty(error).GetProperty("content")
					.TryGetProperty("application/problem+json", out _));
		}

		var list = Resolve(root, Response(collection.GetProperty("get")));
		var dto = Resolve(root, list.GetProperty("items"));

		Assert.Equal("array", list.GetProperty("type").GetString());
		Assert.Equal(new[] { "categoryId", "createdAt", "description", "id", "media", "name", "position", "updatedAt" },
			dto.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order());
		Assert.False(item.GetProperty("delete").GetProperty("responses").GetProperty("204").TryGetProperty("content", out _));
		Assert.Equal(2, item.GetProperty("patch").GetProperty("requestBody").GetProperty("content")
			.EnumerateObject().Count());
		Assert.True(item.GetProperty("patch").GetProperty("responses").TryGetProperty("415", out _));
	}

	[Fact]
	public async Task DocumentsValidationOptionalCoordinatesAndExactUnionMappings()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var collection = root.GetProperty("paths").GetProperty(CollectionPath);
		var item = root.GetProperty("paths").GetProperty(CollectionPath + "/{id}");
		var create = Resolve(root, Request(collection.GetProperty("post")));
		var patch = Resolve(root, Request(item.GetProperty("patch")));
		var dto = Resolve(root, Response(collection.GetProperty("post")));
		var position = Resolve(root, create.GetProperty("properties").GetProperty("position"));

		Assert.Equal(new[] { "categoryId", "name", "position" },
			create.GetProperty("required").EnumerateArray().Select(value => value.GetString()).Order());
		Assert.Equal(new[] { "x", "y" },
			position.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
		Assert.False(patch.TryGetProperty("required", out var required) && required.GetArrayLength() > 0);
		var patchPosition = Resolve(root, patch.GetProperty("properties").GetProperty("position"));

		Assert.False(patchPosition.TryGetProperty("required", out var coordinates) && coordinates.GetArrayLength() > 0);
		Assert.False(patch.GetProperty("additionalProperties").GetBoolean());
		Assert.DoesNotContain("isSpecified", patch.GetRawText());
		Assert.Equal(200, patch.GetProperty("properties").GetProperty("name").GetProperty("maxLength").GetInt32());
		Assert.Equal(5000, create.GetProperty("properties").GetProperty("description").GetProperty("maxLength").GetInt32());
		Assert.Contains(patch.GetProperty("properties").GetProperty("description").GetProperty("type").EnumerateArray(),
			type => type.GetString() == "null");

		foreach (var owner in new[] { create, patch, dto })
		{
			var media = Resolve(root, owner.GetProperty("properties").GetProperty("media").GetProperty("items"));
			var styles = media.GetProperty("anyOf").EnumerateArray().ToArray();
			var discriminator = media.GetProperty("discriminator");
			var mappings = discriminator.GetProperty("mapping");

			Assert.False(media.TryGetProperty("oneOf", out _));
			Assert.Contains(media.GetProperty("required").EnumerateArray(), value => value.GetString() == "type");
			Assert.Equal("type", discriminator.GetProperty("propertyName").GetString());
			Assert.Equal(3, styles.Length);
			Assert.Equal(new[] { "image", "video", "youtubeVideo" },
				mappings.EnumerateObject().Select(property => property.Name).Order());

			foreach (var mapping in mappings.EnumerateObject())
			{
				var style = Assert.Single(styles, value =>
					value.GetProperty("$ref").GetString() == mapping.Value.GetString());
				var properties = Resolve(root, style).GetProperty("properties");

				Assert.Equal(mapping.Name, properties.GetProperty("type").GetProperty("enum")[0].GetString());
				Assert.False(properties.TryGetProperty("id", out _));
				Assert.False(properties.TryGetProperty("markerId", out _));
				Assert.False(properties.TryGetProperty("name", out _));
			}
		}

		var requestMedia = Resolve(root, create.GetProperty("properties").GetProperty("media").GetProperty("items"));
		var requestStyles = requestMedia.GetProperty("anyOf").EnumerateArray()
			.Select(value => Resolve(root, value)).ToArray();
		var image = requestStyles.Single(value => value.GetProperty("properties").TryGetProperty("width", out _));
		var imageProperties = image.GetProperty("properties");

		Assert.Equal("", imageProperties.GetProperty("alt").GetProperty("default").GetString());
		Assert.Equal("string", imageProperties.GetProperty("alt").GetProperty("type").GetString());
		Assert.Equal(2048, imageProperties.GetProperty("url").GetProperty("maxLength").GetInt32());
		Assert.Equal(1, imageProperties.GetProperty("url").GetProperty("minLength").GetInt32());
		Assert.Equal(0, imageProperties.GetProperty("width").GetProperty("exclusiveMinimum").GetInt32());
		Assert.Contains(image.GetProperty("required").EnumerateArray(), value => value.GetString() == "height");
		var youtube = requestStyles.Single(value => value.GetProperty("properties").TryGetProperty("videoId", out _));
		var videoId = youtube.GetProperty("properties").GetProperty("videoId");

		Assert.Equal(11, videoId.GetProperty("minLength").GetInt32());
		Assert.Equal(11, videoId.GetProperty("maxLength").GetInt32());
		Assert.True(videoId.TryGetProperty("pattern", out _));
		var video = requestStyles.Single(value => value.GetProperty("properties").TryGetProperty("preview", out _));

		Assert.Equal(2048, video.GetProperty("properties").GetProperty("preview").GetProperty("maxLength").GetInt32());
	}

	private static JsonElement Request(JsonElement operation) =>
		operation.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema");

	private static JsonElement Response(JsonElement operation) =>
		operation.GetProperty("responses").GetProperty("200").GetProperty("content")
			.GetProperty("application/json").GetProperty("schema");

	private static JsonElement Resolve(JsonElement root, JsonElement schema)
	{
		if (schema.TryGetProperty("$ref", out var reference))
			return Resolve(root, root.GetProperty("components").GetProperty("schemas")
				.GetProperty(reference.GetString()!.Split('/')[^1]));

		if (schema.TryGetProperty("oneOf", out var styles) && !schema.TryGetProperty("discriminator", out _))
			return Resolve(root, Assert.Single(styles.EnumerateArray(), value =>
				!value.TryGetProperty("type", out var type) || type.GetString() != "null"));

		return schema;
	}
}
