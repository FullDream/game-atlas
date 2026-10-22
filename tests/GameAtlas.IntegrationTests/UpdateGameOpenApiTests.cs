using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class UpdateGameOpenApiTests
{
	[Fact]
	public async Task DocumentsBothMediaTypesOptionalFieldsAndValidationConstraints()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var path = root.GetProperty("paths").GetProperty("/games/{id}");
		var operation = path.GetProperty("patch");
		var content = operation.GetProperty("requestBody").GetProperty("content");
		var schema = Resolve(root, content.GetProperty("application/json").GetProperty("schema"));
		var mergeSchema = Resolve(root, content.GetProperty("application/merge-patch+json").GetProperty("schema"));
		var properties = schema.GetProperty("properties");

		Assert.Equal("UpdateGame", operation.GetProperty("operationId").GetString());
		Assert.Equal(2, content.EnumerateObject().Count());
		Assert.Equal(schema.GetRawText(), mergeSchema.GetRawText());
		Assert.False(path.TryGetProperty("put", out _));
		Assert.Equal(5, properties.EnumerateObject().Count());
		Assert.False(schema.TryGetProperty("required", out var required) && required.GetArrayLength() > 0);
		Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
		Assert.Contains("at least one", operation.GetProperty("description").GetString());
		Assert.Contains("read-only", operation.GetProperty("description").GetString());
		Assert.Contains("Merge Patch", operation.GetProperty("description").GetString());
		var id = operation.GetProperty("parameters").EnumerateArray().Single(parameter =>
			parameter.GetProperty("name").GetString() == "id");

		Assert.Equal("path", id.GetProperty("in").GetString());
		Assert.True(id.GetProperty("required").GetBoolean());
		Assert.Equal("uuid", id.GetProperty("schema").GetProperty("format").GetString());

		foreach (var (field, limit) in new[] { ("name", 200), ("slug", 200), ("image", 2048) })
		{
			var fieldSchema = Resolve(root, properties.GetProperty(field));

			Assert.Equal(limit, fieldSchema.GetProperty("maxLength").GetInt32());
			Assert.Equal(1, fieldSchema.GetProperty("minLength").GetInt32());
			Assert.False(AllowsNull(root, fieldSchema));
		}

		Assert.Equal(@"\A[a-z0-9]+(-[a-z0-9]+)*\z", Resolve(root, properties.GetProperty("slug"))
			.GetProperty("pattern").GetString());
		Assert.True(AllowsNull(root, properties.GetProperty("mapId")));
		Assert.False(AllowsNull(root, properties.GetProperty("status")));
		var status = Resolve(root, properties.GetProperty("status"));

		Assert.Equal(new[] { "draft", "published" }, status.GetProperty("enum").EnumerateArray()
			.Select(value => value.GetString()));
		Assert.DoesNotContain("isSpecified", schema.GetRawText());
	}

	[Fact]
	public async Task DocumentsCompleteResponseAndEveryProblemStatusWithoutChangingExistingOperations()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var paths = root.GetProperty("paths");
		var responses = paths.GetProperty("/games/{id}").GetProperty("patch").GetProperty("responses");
		var dto = Resolve(root, responses.GetProperty("200").GetProperty("content")
			.GetProperty("application/json").GetProperty("schema"));

		Assert.Equal(new[] { "createdAt", "id", "image", "mapId", "name", "slug", "status", "updatedAt" },
			dto.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order());

		foreach (var code in new[] { "400", "404", "409", "415", "422", "500" })
			Assert.True(responses.GetProperty(code).GetProperty("content").TryGetProperty("application/problem+json", out _));

		Assert.Equal("CreateGame", paths.GetProperty("/games").GetProperty("post").GetProperty("operationId").GetString());
		Assert.Equal("ListGames", paths.GetProperty("/games").GetProperty("get").GetProperty("operationId").GetString());
		var createSchema = Resolve(root, paths.GetProperty("/games").GetProperty("post")
			.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));

		Assert.Equal(4, createSchema.GetProperty("properties").EnumerateObject().Count());
		Assert.Equal(new[] { "draft", "published" }, Resolve(root, dto.GetProperty("properties").GetProperty("status"))
			.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
		Assert.False(AllowsNull(root, dto.GetProperty("properties").GetProperty("status")));
	}

	private static JsonElement Resolve(JsonElement root, JsonElement schema)
	{
		while (schema.TryGetProperty("$ref", out var reference))
			schema = root.GetProperty("components").GetProperty("schemas").GetProperty(reference.GetString()!.Split('/')[^1]);

		return schema;
	}

	private static bool AllowsNull(JsonElement root, JsonElement schema)
	{
		schema = Resolve(root, schema);

		if (schema.TryGetProperty("type", out var type))
		{
			if (type.ValueKind == JsonValueKind.String && type.GetString() == "null")
				return true;

			if (type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(value => value.GetString() == "null"))
				return true;
		}

		return schema.TryGetProperty("anyOf", out var options) &&
			options.EnumerateArray().Any(option => AllowsNull(root, option));
	}
}
