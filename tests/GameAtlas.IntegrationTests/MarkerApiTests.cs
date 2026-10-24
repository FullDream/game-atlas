using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MarkerApiTests
{
	[Theory]
	[InlineData(GameStatus.Draft)]
	[InlineData(GameStatus.Published)]
	public async Task MixedMediaCrudPreservesParentsAndCounts(GameStatus status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await DeleteGameHandlerTests.SeedAsync(factory, Guid.NewGuid().ToString(), status);
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var created = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));
		var id = created.GetProperty("id").GetGuid();
		var before = await MarkerTestData.LoadAsync(factory, id);

		Assert.Equal(8, created.EnumerateObject().Count());
		Assert.Equal(7, id.Version);
		Assert.Equal("Description", created.GetProperty("description").GetString());
		Assert.Equal(created.GetProperty("createdAt").GetDateTimeOffset(),
			created.GetProperty("updatedAt").GetDateTimeOffset());
		Assert.Equal(new[] { "image", "image", "video" },
			created.GetProperty("media").EnumerateArray().Select(item => item.GetProperty("type").GetString()));
		Assert.Equal("", created.GetProperty("media")[0].GetProperty("alt").GetString());
		Assert.Equal(MediaTestData.PreviewId, created.GetProperty("media")[2].GetProperty("preview").GetProperty("id").GetGuid());
		Assert.DoesNotContain("markerId", created.GetRawText());
		Assert.Equal(3, before.Media.Count);

		using var noOpResponse = await client.PatchAsJsonAsync(MarkerTestData.Path(game.Id) + $"/{id}",
			new { name = " Marker ", position = new { }, media = created.GetProperty("media").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToArray() });
		var noOp = await MarkerTestData.ReadAsync(noOpResponse);
		var unchanged = await MarkerTestData.LoadAsync(factory, id);

		Assert.Equal(created.GetRawText(), noOp.GetRawText());
		Assert.Equal(before.Media.Select(item => item.AssetId), unchanged.Media.Select(item => item.AssetId));

		using var xResponse = await client.PatchAsync(MarkerTestData.Path(game.Id) + $"/{id}",
			new StringContent("""{"position":{"x":200},"description":null}""", Encoding.UTF8,
				"application/merge-patch+json"));
		var updated = await MarkerTestData.ReadAsync(xResponse);

		Assert.Equal(200, updated.GetProperty("position").GetProperty("x").GetDouble());
		Assert.Equal(0, updated.GetProperty("position").GetProperty("y").GetDouble());
		Assert.Equal(JsonValueKind.Null, updated.GetProperty("description").ValueKind);
		using var yResponse = await client.PatchAsJsonAsync(MarkerTestData.Path(game.Id) + $"/{id}",
			new { position = new { y = -50.25 } });
		var yUpdated = await MarkerTestData.ReadAsync(yResponse);

		Assert.Equal(200, yUpdated.GetProperty("position").GetProperty("x").GetDouble());
		Assert.Equal(-50.25, yUpdated.GetProperty("position").GetProperty("y").GetDouble());
		Assert.Equal(created.GetProperty("createdAt").GetDateTimeOffset(),
			yUpdated.GetProperty("createdAt").GetDateTimeOffset());

		using var categories = await client.GetAsync(CategoryTestData.Path(game.Id, group.Id));
		var counted = await MarkerTestData.ReadAsync(categories);

		Assert.Equal(1, counted[0].GetProperty("markersCount").GetInt32());
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			Assert.Equal(game.UpdatedAt, (await db.Games.SingleAsync()).UpdatedAt);
			Assert.Equal(group.UpdatedAt, (await db.CategoryGroups.SingleAsync()).UpdatedAt);
			Assert.Equal(category.UpdatedAt, (await db.Categories.SingleAsync()).UpdatedAt);
		}

		using var deleted = await client.DeleteAsync(MarkerTestData.Path(game.Id) + $"/{id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Empty(await deleted.Content.ReadAsByteArrayAsync());
		using var repeated = await client.DeleteAsync(MarkerTestData.Path(game.Id) + $"/{id}");

		Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode);
		await using var finalScope = factory.Services.CreateAsyncScope();
		var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Empty(await finalDb.Markers.ToArrayAsync());
		Assert.Equal(0, await finalDb.Set<GameAtlas.Domain.Markers.MarkerAsset>().CountAsync());
	}

	[Theory]
	[InlineData("{}", 422)]
	[InlineData("null", 400)]
	[InlineData("[]", 400)]
	[InlineData("{", 400)]
	[InlineData("""{"name":null}""", 422)]
	[InlineData("""{"name":""}""", 422)]
	[InlineData("""{"categoryId":null}""", 422)]
	[InlineData("""{"categoryId":"invalid"}""", 400)]
	[InlineData("""{"position":null}""", 422)]
	[InlineData("""{"position":{"x":null}}""", 422)]
	[InlineData("""{"position":{"y":null}}""", 422)]
	[InlineData("""{"position":{"x":"1"}}""", 400)]
	[InlineData("""{"position":{"x":1e999}}""", 422)]
	[InlineData("""{"position":{"z":1}}""", 400)]
	[InlineData("""{"id":"00000000-0000-0000-0000-000000000000"}""", 400)]
	[InlineData("""{"createdAt":"2026-01-01T00:00:00Z"}""", 400)]
	[InlineData("""{"media":null}""", 422)]
	[InlineData("""{"media":[null]}""", 422)]
	[InlineData("""{"media":[{}]}""", 400)]
	[InlineData("""{"media":[{"type":"other"}]}""", 400)]
	[InlineData("""{"media":[{"type":"Image","url":"a","width":1,"height":1}]}""", 400)]
	[InlineData("""{"media":[{"type":"image"}]}""", 400)]
	[InlineData("""{"media":[{"type":"image","url":"a","width":0,"height":1}]}""", 400)]
	[InlineData("""{"media":[{"type":"image","url":"a","width":1.5,"height":1}]}""", 400)]
	[InlineData("""{"media":[{"type":"image","url":"a","width":2147483648,"height":1}]}""", 400)]
	[InlineData("""{"media":[{"type":"image","url":"a","width":1,"height":1,"alt":null}]}""", 400)]
	[InlineData("""{"media":[{"type":"youtubeVideo","videoId":"https://youtube.com/watch?v=Ab_cd-Ef012"}]}""", 400)]
	[InlineData("""{"media":[{"type":"youtubeVideo","videoId":"Ab_cd-Ef012","url":"wrong"}]}""", 400)]
	[InlineData("""{"media":[{"type":"video","url":"v","width":1}]}""", 400)]
	[InlineData("""{"media":[{"type":"video","url":"v","id":"00000000-0000-0000-0000-000000000000"}]}""", 400)]
	[InlineData("""{"media":[{"type":"video","url":"v","name":"caption"}]}""", 400)]
	public async Task RejectedPatchIsAtomic(string body, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var created = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));
		var id = created.GetProperty("id").GetGuid();
		var before = await MarkerTestData.LoadAsync(factory, id);
		using var response = await client.PatchAsync(MarkerTestData.Path(game.Id) + $"/{id}",
			new StringContent(body, Encoding.UTF8, "application/json"));

		Assert.Equal(status, (int)response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		var after = await MarkerTestData.LoadAsync(factory, id);

		Assert.Equal(before.Name, after.Name);
		Assert.Equal(before.Position, after.Position);
		Assert.Equal(before.UpdatedAt, after.UpdatedAt);
		Assert.Equal(before.Media.Select(item => item.AssetId), after.Media.Select(item => item.AssetId));
	}

	[Theory]
	[InlineData("{}")]
	[InlineData("""{"name":"M","position":{"x":0,"y":0}}""")]
	[InlineData("""{"name":"M","position":{"x":0}}""")]
	[InlineData("""{"name":"M","position":{"y":0}}""")]
	[InlineData("""{"name":"M","position":null}""")]
	public async Task CreationRequiresCategoryNameAndBothCoordinates(string body)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var (game, _) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsync(MarkerTestData.Path(game.Id),
			new StringContent(body, Encoding.UTF8, "application/json"));

		Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
	}

	[Theory]
	[InlineData("application/json", 200)]
	[InlineData("application/merge-patch+json", 200)]
	[InlineData("application/json-patch+json", 415)]
	[InlineData("text/plain", 415)]
	public async Task PatchUsesSharedMediaTypeHandling(string mediaType, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var created = await MarkerTestData.CreateAsync(client, game.Id, category.Id);
		using var response = await client.PatchAsync(MarkerTestData.Path(game.Id) + "/" + created.GetProperty("id").GetGuid(),
			new StringContent("""{"position":{}}""", Encoding.UTF8, mediaType));

		Assert.Equal(status, (int)response.StatusCode);
	}
}
