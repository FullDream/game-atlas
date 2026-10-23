using System.Text.Json;

namespace GameAtlas.IntegrationTests;

public class DeleteGameOpenApiTests
{
	[Fact]
	public async Task DocumentsEmpty204GuidIdentifierAndProblemResponses()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var paths = document.RootElement.GetProperty("paths");
		var operation = paths.GetProperty("/games/{id}").GetProperty("delete");
		var id = Assert.Single(operation.GetProperty("parameters").EnumerateArray());
		var responses = operation.GetProperty("responses");

		Assert.Equal("DeleteGame", operation.GetProperty("operationId").GetString());
		Assert.Equal("Delete game", operation.GetProperty("summary").GetString());
		Assert.Equal("id", id.GetProperty("name").GetString());
		Assert.Equal("path", id.GetProperty("in").GetString());
		Assert.True(id.GetProperty("required").GetBoolean());
		Assert.Equal("uuid", id.GetProperty("schema").GetProperty("format").GetString());
		Assert.False(operation.TryGetProperty("requestBody", out _));
		Assert.False(responses.GetProperty("204").TryGetProperty("content", out _));
		Assert.False(responses.TryGetProperty("200", out _));
		Assert.False(responses.TryGetProperty("409", out _));
		Assert.False(responses.TryGetProperty("422", out _));

		foreach (var status in new[] { "400", "404", "500" })
			Assert.True(responses.GetProperty(status).GetProperty("content")
				.TryGetProperty("application/problem+json", out _));

		Assert.Equal("CreateGame", paths.GetProperty("/games").GetProperty("post")
			.GetProperty("operationId").GetString());
		Assert.Equal("ListGames", paths.GetProperty("/games").GetProperty("get")
			.GetProperty("operationId").GetString());
		Assert.Equal("UpdateGame", paths.GetProperty("/games/{id}").GetProperty("patch")
			.GetProperty("operationId").GetString());
		Assert.False(paths.GetProperty("/games/{id}").TryGetProperty("put", out _));
	}
}
