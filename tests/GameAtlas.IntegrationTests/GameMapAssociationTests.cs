using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class GameMapAssociationTests
{
	[Theory]
	[InlineData("null")]
	[InlineData("\"invalid-guid\"")]
	[InlineData("\"019a0000-0000-7000-8000-000000000001\"")]
	[InlineData("123")]
	[InlineData("true")]
	[InlineData("{}")]
	[InlineData("[]")]
	public async Task CreationIgnoresEveryAdditionalMapIdRepresentationAndPatchRejectsIt(string mapId)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureCoverAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var created = await client.PostAsync("/games", new StringContent(
			"{\"name\":\"Game\",\"slug\":\"game\",\"imageAssetId\":\"019a0000-0000-7000-8000-000000000115\",\"mapId\":" + mapId + "}",
			Encoding.UTF8, "application/json"));
		var game = await created.Content.ReadFromJsonAsync<JsonElement>();

		Assert.Equal(HttpStatusCode.OK, created.StatusCode);
		Assert.False(game.TryGetProperty("mapId", out _));
		var id = game.GetProperty("id").GetGuid();
		var map = await MapTestData.SeedAsync(factory, id);
		using var patched = await client.PatchAsync($"/games/{id}", new StringContent(
			"{\"name\":\"Changed\",\"mapId\":" + mapId + "}", Encoding.UTF8, "application/json"));

		Assert.Equal(HttpStatusCode.BadRequest, patched.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Equal("Game", (await context.Games.SingleAsync()).Name);
		Assert.Equal(map.Id, (await context.Maps.SingleAsync()).Id);
	}
}
