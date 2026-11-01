using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Maps;

namespace GameAtlas.IntegrationTests;

public class UpdateMapTests
{
	[Fact]
	public async Task RenameReturnsFullMapAndPreservesOwnedContentAndNoOpTimestamp()
	{
		await using var factory = new GameApiFactory("Development");
		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var beforeRead = await client.GetAsync(MapTestData.PathFor(game.Id));
		var before = await MapTestData.ReadAsync(beforeRead);
		using var response = await client.PatchAsJsonAsync(MapTestData.PathFor(game.Id), new { name = " Renamed " });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var after = await MapTestData.ReadAsync(response);

		Assert.Equal("Renamed", after.Name);
		Assert.Equal(before.CreatedAt, after.CreatedAt);
		Assert.Equal(before.Extent, after.Extent);
		Assert.Equal(before.Width, after.Width);
		Assert.Equal(before.Height, after.Height);
		Assert.Equal(before.Styles, after.Styles);
		Assert.True(after.UpdatedAt >= before.UpdatedAt);
		using var noOp = await client.PatchAsJsonAsync(MapTestData.PathFor(game.Id), new { name = " Renamed " });

		Assert.Equal(after.UpdatedAt, (await MapTestData.ReadAsync(noOp)).UpdatedAt);
		using var groupRead = await client.GetAsync($"{MapTestData.PathFor(game.Id)}/category-groups");
		var groups = await groupRead.Content.ReadFromJsonAsync<JsonElement>();

		Assert.Contains(groups.EnumerateArray(), item => item.GetProperty("id").GetGuid() == group.Id);
		using var gameRead = await client.GetAsync($"/games/{game.Id}");
		var persistedGame = await gameRead.Content.ReadFromJsonAsync<JsonElement>();

		Assert.Equal(game.UpdatedAt, persistedGame.GetProperty("updatedAt").GetDateTimeOffset());
	}

	[Theory]
	[InlineData("{}", "application/json", 422)]
	[InlineData("{\"name\":null}", "application/json", 422)]
	[InlineData("{\"name\":\"  \"}", "application/json", 422)]
	[InlineData("{\"width\":100}", "application/json", 400)]
	[InlineData("{\"name\":17}", "application/json", 400)]
	[InlineData("[]", "application/json", 400)]
	[InlineData("{", "application/json", 400)]
	[InlineData("{\"name\":\"Next\"}", "text/plain", 415)]
	[InlineData("{\"name\":\"Next\"}", "application/json; charset=utf-8", 200)]
	[InlineData("{\"name\":\"Next\"}", "application/merge-patch+json; charset=utf-8", 200)]
	public async Task EnforcesPatchContract(string json, string mediaType, int status)
	{
		await using var factory = new GameApiFactory("Development");
		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "patch-contract");
		await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var body = new StringContent(json, Encoding.UTF8);
		body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType);
		using var response = await client.PatchAsync(MapTestData.PathFor(game.Id), body);

		Assert.Equal(status, (int)response.StatusCode);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(status == 200 ? "Next" : "World", (await MapTestData.ReadAsync(read)).Name);
	}

	[Fact]
	public async Task RejectsForeignMapsAndConcurrentDuplicateNamesAndExposesOpenApi()
	{
		await using var factory = new GameApiFactory("Development");
		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "rename-game");
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other-game");
		var map = await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var foreign = await client.PatchAsJsonAsync(MapTestData.PathFor(other.Id, map.Id), new { name = "Next" });
		using var missing = await client.PatchAsJsonAsync(MapTestData.PathFor(game.Id, Guid.CreateVersion7()), new { name = "Next" });

		Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		using var siblingCreation = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "Sibling", width = 1, height = 1 });
		var sibling = await MapTestData.ReadAsync(siblingCreation);
		var responses = await Task.WhenAll(
			client.PatchAsJsonAsync(MapTestData.PathFor(game.Id, map.Id), new { name = "Shared" }),
			client.PatchAsJsonAsync(MapTestData.PathFor(game.Id, sibling.Id), new { name = " shared " }));

		try
		{
			Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
			Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
		}
		finally
		{
			foreach (var response in responses)
				response.Dispose();
		}

		using var schemaRead = await client.GetAsync("/openapi/v1.json");
		var document = await schemaRead.Content.ReadFromJsonAsync<JsonElement>();
		var operation = document.GetProperty("paths").GetProperty("/games/{gameId}/maps/{mapId}").GetProperty("patch");

		Assert.Equal("UpdateMap", operation.GetProperty("operationId").GetString());
		var schema = document.GetProperty("components").GetProperty("schemas").GetProperty("UpdateMapRequest");

		Assert.True(schema.GetProperty("properties").TryGetProperty("name", out _));
		Assert.False(schema.GetProperty("properties").TryGetProperty("width", out _));
		Assert.False(schema.TryGetProperty("required", out var required) && required.EnumerateArray().Any(value => value.GetString() == "name"));
	}
}
