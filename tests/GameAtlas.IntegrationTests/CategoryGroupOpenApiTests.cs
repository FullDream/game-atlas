using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class CategoryGroupOpenApiTests
{
	[Fact]
	public async Task DocumentsFourOperationsWithoutReorderOrIndividualGet()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

		var root = document.RootElement;

		var paths = root.GetProperty("paths");

		var collection = paths.GetProperty("/games/{gameId}/maps/{mapId}/category-groups");

		var item = paths.GetProperty("/games/{gameId}/maps/{mapId}/category-groups/{id}");

		var operations = new[]
		{
			(collection.GetProperty("post"), "CreateCategoryGroup", new[] { "400", "404", "409", "422", "500" }),
			(collection.GetProperty("get"), "ListCategoryGroups", new[] { "400", "404", "500" }),
			(item.GetProperty("patch"), "UpdateCategoryGroup", new[] { "400", "404", "409", "415", "422", "500" }),
			(item.GetProperty("delete"), "DeleteCategoryGroup", new[] { "400", "404", "500" })
		};

		foreach (var (operation, id, errors) in operations)
		{
			CategoryGroupTestData.AssertSame(id, operation.GetProperty("operationId").GetString());

			foreach (var parameter in operation.GetProperty("parameters").EnumerateArray())
			{
				CategoryGroupTestData.AssertSame("path", parameter.GetProperty("in").GetString());
				Assert.True(parameter.GetProperty("required").GetBoolean());
				CategoryGroupTestData.AssertSame("uuid", parameter.GetProperty("schema").GetProperty("format").GetString());
			}

			foreach (var status in errors)
				Assert.True(operation.GetProperty("responses").GetProperty(status).GetProperty("content")
					.TryGetProperty("application/problem+json", out _));
		}

		Assert.False(paths.TryGetProperty("/games/{gameId}/maps/{mapId}/category-groups/order", out _));
		Assert.False(item.TryGetProperty("get", out _));
		Assert.False(item.TryGetProperty("put", out _));
		CategoryGroupTestData.AssertSame(2, collection.GetProperty("get").GetProperty("parameters").GetArrayLength());
		Assert.False(item.GetProperty("delete").GetProperty("responses").GetProperty("204")
			.TryGetProperty("content", out _));

		var list = Resolve(root, collection.GetProperty("get").GetProperty("responses").GetProperty("200")
			.GetProperty("content").GetProperty("application/json").GetProperty("schema"));

		CategoryGroupTestData.AssertSame("array", list.GetProperty("type").GetString());
		AssertDto(root, list.GetProperty("items"));

		foreach (var operation in new[] { collection.GetProperty("post"), item.GetProperty("patch") })
			AssertDto(root, operation.GetProperty("responses").GetProperty("200")
				.GetProperty("content").GetProperty("application/json").GetProperty("schema"));
	}

	[Fact]
	public async Task DocumentsRelativePlacementOptionalIconsAndNameValidation()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

		var root = document.RootElement;

		var paths = root.GetProperty("paths");

		var post = paths.GetProperty("/games/{gameId}/maps/{mapId}/category-groups").GetProperty("post");

		var patch = paths.GetProperty("/games/{gameId}/maps/{mapId}/category-groups/{id}").GetProperty("patch");

		var patchContent = patch.GetProperty("requestBody").GetProperty("content");

		var patchSchema = Resolve(root, patchContent.GetProperty("application/json").GetProperty("schema"));

		CategoryGroupTestData.AssertSame(2, patchContent.EnumerateObject().Count());
		CategoryGroupTestData.AssertSame(patchSchema.GetRawText(), Resolve(root, patchContent.GetProperty("application/merge-patch+json")
			.GetProperty("schema")).GetRawText());
		Assert.False(patchSchema.GetProperty("additionalProperties").GetBoolean());

		foreach (var operation in new[] { post, patch })
		{
			var schema = Resolve(root, operation.GetProperty("requestBody").GetProperty("content")
				.GetProperty("application/json").GetProperty("schema"));

			var properties = schema.GetProperty("properties");

			var name = Resolve(root, properties.GetProperty("name"));

			var order = Resolve(root, properties.GetProperty("beforeId"));
			Assert.False(properties.TryGetProperty("order", out _));
			Assert.Equal(GameAtlas.Domain.Categories.Category.ColorPattern, properties.GetProperty("color").GetProperty("pattern").GetString());

			CategoryGroupTestData.AssertSame(4, properties.EnumerateObject().Count());
			CategoryGroupTestData.AssertSame("string", name.GetProperty("type").GetString());
			CategoryGroupTestData.AssertSame(1, name.GetProperty("minLength").GetInt32());
			CategoryGroupTestData.AssertSame(200, name.GetProperty("maxLength").GetInt32());
			CategoryGroupTestData.AssertSame("uuid", order.GetProperty("format").GetString());
			Assert.False(schema.TryGetProperty("required", out var required) &&
				required.EnumerateArray().Any(value => value.GetString() == "order"));
			Assert.DoesNotContain("isSpecified", schema.GetRawText());
		}

		Assert.False(patchSchema.TryGetProperty("required", out var patchRequired) && patchRequired.GetArrayLength() > 0);
	}

	private static void AssertDto(JsonElement root, JsonElement schema)
	{
		var properties = Resolve(root, schema).GetProperty("properties");
		Assert.False(properties.TryGetProperty("order", out _));
		Assert.True(properties.GetProperty("markersCount").GetProperty("readOnly").GetBoolean());

		CategoryGroupTestData.AssertSame(new[] { "categories", "color", "createdAt", "icon", "id", "mapId", "markersCount", "name", "updatedAt" },
			properties.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
	}

	private static JsonElement Resolve(JsonElement root, JsonElement schema)
	{
		while (schema.TryGetProperty("$ref", out var reference))
			schema = root.GetProperty("components").GetProperty("schemas")
				.GetProperty(reference.GetString()!.Split('/')[^1]);

		return schema;
	}
}
