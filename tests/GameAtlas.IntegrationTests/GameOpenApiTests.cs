using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class GameOpenApiTests
{
	[Fact]
	public async Task DocumentsCreateGameResponseStringStatusAndProblemDetails()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var root = document.RootElement;
		var operation = root.GetProperty("paths").GetProperty("/games").GetProperty("post");
		var responses = operation.GetProperty("responses");

		Assert.Equal("CreateGame", operation.GetProperty("operationId").GetString());
		Assert.True(operation.TryGetProperty("requestBody", out _));

		foreach (var status in new[] { "200", "400", "409", "422", "500" })
			Assert.True(responses.TryGetProperty(status, out _));

		var schemas = root.GetProperty("components").GetProperty("schemas");
		var statusSchema = schemas.GetProperty("GameStatus");

		Assert.All(statusSchema.GetProperty("enum").EnumerateArray(), value =>
			Assert.Equal(JsonValueKind.String, value.ValueKind));
		Assert.Contains(statusSchema.GetProperty("enum").EnumerateArray(), value => value.GetString() == "draft");
		var dto = schemas.GetProperty("GameDto").GetProperty("properties");

		foreach (var property in new[] { "id", "name", "slug", "image", "mapId", "createdAt", "updatedAt", "status" })
			Assert.True(dto.TryGetProperty(property, out _));
	}
}
