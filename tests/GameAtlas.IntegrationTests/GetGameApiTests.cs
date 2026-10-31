using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Games;

namespace GameAtlas.IntegrationTests;

public sealed class GetGameApiTests
{
	[Theory]
	[InlineData("draft")]
	[InlineData("published")]
	public async Task ReturnsCompleteGameWithoutChangingStoredMetadata(string status)
	{
		var capture = new SqlReadCapture();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client);
		using var created = await client.PostAsJsonAsync("/games",
			new { name = "Game", slug = "game", coverId = image.Id });
		var game = (await created.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!;

		if (status == "published")
		{
			using var updated = await client.PatchAsJsonAsync($"/games/{game.Id}", new { status });
			updated.EnsureSuccessStatusCode();
			game = (await updated.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!;
		}

		capture.Enabled = true;
		using var response = await client.GetAsync($"/games/{game.Id}");
		var actual = await response.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions);
		capture.Enabled = false;

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equivalent(game, actual);
		Assert.Equal(image, actual!.Cover);
		using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		Assert.Equal(14, payload.RootElement.EnumerateObject().Count());
		Assert.Empty(payload.RootElement.GetProperty("maps").EnumerateArray());
		Assert.False(payload.RootElement.TryGetProperty("mapId", out _));
		Assert.All(capture.Commands, command => Assert.StartsWith("SELECT", command));
		Assert.Equivalent(game, await client.GetFromJsonAsync<GameDto>($"/games/{game.Id}", MapTestData.JsonOptions));
	}

	[Fact]
	public async Task MissingAndDeletedGamesReturnNotFound()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var missing = await client.GetAsync($"/games/{Guid.NewGuid()}");
		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);

		var image = await MediaTestData.UploadImageAsync(client);
		using var created = await client.PostAsJsonAsync("/games",
			new { name = "Game", slug = "game", coverId = image.Id });
		var game = (await created.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!;
		using var deleted = await client.DeleteAsync($"/games/{game.Id}");
		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		using var response = await client.GetAsync($"/games/{game.Id}");
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task MalformedIdentifierReturnsBadRequest()
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync("/games/not-a-uuid");
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task DocumentsCanonicalGameAndErrors()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var operation = document.RootElement.GetProperty("paths").GetProperty("/games/{id}").GetProperty("get");
		Assert.Equal("GetGame", operation.GetProperty("operationId").GetString());
		Assert.Contains(operation.GetProperty("tags").EnumerateArray(), tag => tag.GetString() == "Games");
		var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray());
		Assert.Equal("uuid", parameter.GetProperty("schema").GetProperty("format").GetString());
		var responses = operation.GetProperty("responses");
		Assert.Equal("#/components/schemas/Game", responses.GetProperty("200").GetProperty("content")
			.GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString());

		foreach (var status in new[] { "400", "404", "500" })
			Assert.True(responses.GetProperty(status).GetProperty("content")
				.TryGetProperty("application/problem+json", out _));
	}
}
