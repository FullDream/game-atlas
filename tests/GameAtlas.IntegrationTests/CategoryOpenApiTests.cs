using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class CategoryOpenApiTests
{
	private const string CollectionPath = "/games/{gameId}/maps/{mapId}/category-groups/{groupId}/categories";

	[Fact]
	public async Task DocumentsCategoryMutationsAndTreeChildrenWithErrors()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

		var root = document.RootElement;

		var paths = root.GetProperty("paths");

		var collection = paths.GetProperty(CollectionPath);

		var item = paths.GetProperty(CollectionPath + "/{id}");

		var operations = new[]
		{
			(collection.GetProperty("post"), "CreateCategory", new[] { "400", "404", "409", "422", "500" }),
			(item.GetProperty("patch"), "UpdateCategory", new[] { "400", "404", "409", "415", "422", "500" }),
			(item.GetProperty("delete"), "DeleteCategory", new[] { "400", "404", "500" })
		};

		foreach (var (operation, id, errors) in operations)
		{
			Assert.Equal(id, operation.GetProperty("operationId").GetString());

			foreach (var parameter in operation.GetProperty("parameters").EnumerateArray())
			{
				Assert.Equal("path", parameter.GetProperty("in").GetString());
				Assert.True(parameter.GetProperty("required").GetBoolean());
				Assert.Equal("uuid", parameter.GetProperty("schema").GetProperty("format").GetString());
			}

			foreach (var status in errors)
				Assert.True(operation.GetProperty("responses").GetProperty(status).GetProperty("content")
					.TryGetProperty("application/problem+json", out _));
		}

		Assert.Single(collection.EnumerateObject());
		Assert.Equal(2, item.EnumerateObject().Count());
		Assert.False(paths.TryGetProperty(CollectionPath + "/order", out _));
		Assert.False(paths.TryGetProperty(CollectionPath + "/batch", out _));
		Assert.False(collection.TryGetProperty("get", out _));
		Assert.False(item.GetProperty("delete").GetProperty("responses").GetProperty("204")
			.TryGetProperty("content", out _));

		var list = ResponseSchema(root, paths.GetProperty("/games/{gameId}/maps/{mapId}/category-groups").GetProperty("get"));

		Assert.Equal("array", list.GetProperty("type").GetString());

		var groupSchema = Resolve(root, list.GetProperty("items"));
		AssertDto(root, groupSchema.GetProperty("properties").GetProperty("categories").GetProperty("items"));
		AssertDto(root, ResponseSchema(root, collection.GetProperty("post")));
		AssertDto(root, ResponseSchema(root, item.GetProperty("patch")));
	}

	[Fact]
	public async Task DocumentsSharedValidationAndOptionalPatchFields()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

		var root = document.RootElement;

		var paths = root.GetProperty("paths");

		var post = paths.GetProperty(CollectionPath).GetProperty("post");

		var patch = paths.GetProperty(CollectionPath + "/{id}").GetProperty("patch");

		var patchContent = patch.GetProperty("requestBody").GetProperty("content");

		var patchSchema = Resolve(root, patchContent.GetProperty("application/json").GetProperty("schema"));

		Assert.Equal(2, patchContent.EnumerateObject().Count());
		Assert.Equal(patchSchema.GetRawText(), Resolve(root, patchContent.GetProperty("application/merge-patch+json")
			.GetProperty("schema")).GetRawText());
		Assert.False(patchSchema.GetProperty("additionalProperties").GetBoolean());
		Assert.False(patchSchema.TryGetProperty("required", out var required) && required.GetArrayLength() > 0);
		Assert.DoesNotContain("isSpecified", patchSchema.GetRawText());

		foreach (var operation in new[] { post, patch })
		{
			var schema = Resolve(root, operation.GetProperty("requestBody").GetProperty("content")
				.GetProperty("application/json").GetProperty("schema"));

			var properties = schema.GetProperty("properties");

			foreach (var (field, maximum) in new[] { ("name", 200) })
			{
				var value = Resolve(root, properties.GetProperty(field));

				Assert.Equal("string", value.GetProperty("type").GetString());
				Assert.Equal(1, value.GetProperty("minLength").GetInt32());
				Assert.Equal(maximum, value.GetProperty("maxLength").GetInt32());
			}

			Assert.False(properties.TryGetProperty("icon", out _));
			Assert.Equal("uuid", Resolve(root, properties.GetProperty("iconAssetId")).GetProperty("format").GetString());

			var color = Resolve(root, properties.GetProperty("color"));

			var order = Resolve(root, properties.GetProperty("beforeId"));
			Assert.False(properties.TryGetProperty("order", out _));

			Assert.Equal(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})(?![\s\S])",
				color.GetProperty("pattern").GetString());
			Assert.Equal(4, color.GetProperty("minLength").GetInt32());
			Assert.Equal(7, color.GetProperty("maxLength").GetInt32());
			Assert.Equal("uuid", order.GetProperty("format").GetString());
			Assert.False(schema.TryGetProperty("required", out var requiredFields) &&
				requiredFields.EnumerateArray().Any(value => value.GetString() == "order"));
		}

		Assert.Equal("uuid", Resolve(root, patchSchema.GetProperty("properties").GetProperty("categoryGroupId"))
			.GetProperty("format").GetString());
	}

	private static JsonElement ResponseSchema(JsonElement root, JsonElement operation) =>
		Resolve(root, operation.GetProperty("responses").GetProperty("200").GetProperty("content")
			.GetProperty("application/json").GetProperty("schema"));

	private static void AssertDto(JsonElement root, JsonElement schema)
	{
		var properties = Resolve(root, schema).GetProperty("properties");

		Assert.Equal(new[]
			{
				"categoryGroupId", "color", "createdAt", "icon", "id", "markersCount", "name", "updatedAt"
			},
			properties.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
		Assert.Equal("integer", Resolve(root, properties.GetProperty("markersCount")).GetProperty("type").GetString());
		Assert.False(properties.TryGetProperty("order", out _));
		Assert.True(properties.GetProperty("markersCount").GetProperty("readOnly").GetBoolean());
	}

	private static JsonElement Resolve(JsonElement root, JsonElement schema)
	{
		while (schema.TryGetProperty("$ref", out var reference))
			schema = root.GetProperty("components").GetProperty("schemas")
				.GetProperty(Assert.IsType<string>(reference.GetString()).Split('/')[^1]);

		return schema;
	}
}
