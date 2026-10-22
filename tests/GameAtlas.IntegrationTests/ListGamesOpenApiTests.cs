using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class ListGamesOpenApiTests
{
	[Fact]
	public async Task DocumentsQueryDefaultsBoundsAndSharedPaginatedResponse()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var operation = root.GetProperty("paths").GetProperty("/games").GetProperty("get");
		var parameters = operation.GetProperty("parameters").EnumerateArray()
			.ToDictionary(parameter => parameter.GetProperty("name").GetString()!, StringComparer.OrdinalIgnoreCase);

		Assert.Equal("ListGames", operation.GetProperty("operationId").GetString());
		Assert.Equal("List games", operation.GetProperty("summary").GetString());
		Assert.Equal(new[] { "limit", "offset", "search", "sortby", "sortdirection" },
			parameters.Keys.Select(name => name.ToLowerInvariant()).Order());
		var offset = parameters["offset"].GetProperty("schema");
		var limit = parameters["limit"].GetProperty("schema");

		Assert.Equal(0, offset.GetProperty("default").GetInt32());
		Assert.Equal(0, offset.GetProperty("minimum").GetInt32());
		Assert.False(offset.TryGetProperty("maximum", out _));
		Assert.Equal(20, limit.GetProperty("default").GetInt32());
		Assert.Equal(1, limit.GetProperty("minimum").GetInt32());
		Assert.Equal(100, limit.GetProperty("maximum").GetInt32());

		foreach (var status in new[] { "200", "400", "422", "500" })
			Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));

		var schemas = root.GetProperty("components").GetProperty("schemas");
		var reference = operation.GetProperty("responses").GetProperty("200").GetProperty("content")
			.GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
		var properties = schemas.GetProperty(reference.Split('/').Last()).GetProperty("properties");

		Assert.Equal(new[] { "items", "limit", "offset", "total" },
			properties.EnumerateObject().Select(property => property.Name).Order());
		Assert.Contains("GameDto", properties.GetProperty("items").GetProperty("items")
			.GetProperty("$ref").GetString());
	}
}
