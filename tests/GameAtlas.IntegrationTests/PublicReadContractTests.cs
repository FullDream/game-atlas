using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Games;
using GameAtlas.Application.Maps;
using GameAtlas.Domain;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.ValueObjects;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class PublicReadContractTests
{
	[Fact]
	public async Task PublicSchemasAndTagsAreIndependentAndReuseCanonicalMedia()
	{
		await using var factory = new GameApiFactory("Development");
		using var client = factory.CreateHttpsClient();
		using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
		var paths = document.RootElement.GetProperty("paths");
		var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

		foreach (var (path, id, tag) in new[]
		{
			("/public/games", "ListPublicGames", "Public Games"),
			("/public/games/{gameSlug}", "GetPublicGame", "Public Games"),
			("/public/games/{gameSlug}/maps/{mapSlug}", "GetPublicMap", "Public Maps")
		})
		{
			var operation = paths.GetProperty(path).GetProperty("get");

			Assert.Equal(id, operation.GetProperty("operationId").GetString());
			Assert.Equal(tag, operation.GetProperty("tags")[0].GetString());
			Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
			Assert.DoesNotContain(operation.GetProperty("parameters").EnumerateArray(), parameter =>
				parameter.GetProperty("name").GetString() is "visibility" or "includeDrafts");
		}

		foreach (var name in new[] { "PublicGameCard", "PublicGame", "PublicMapDetails", "PublicMapStyle",
			"PublicCategoryGroup", "PublicCategory", "PublicMarker" })
		{
			var properties = schemas.GetProperty(name).GetProperty("properties");

			Assert.False(properties.TryGetProperty("status", out _));
			Assert.False(properties.TryGetProperty("createdAt", out _));
			Assert.False(properties.TryGetProperty("updatedAt", out _));
		}

		Assert.Equal("#/components/schemas/ImageAsset", schemas.GetProperty("PublicGame")
			.GetProperty("properties").GetProperty("cover").GetProperty("$ref").GetString());
		var map = schemas.GetProperty("PublicMapDetails").GetProperty("properties");
		var summary = schemas.GetProperty("PublicGameMap").GetProperty("properties");

		Assert.Equal("integer", summary.GetProperty("markersCount").GetProperty("type").GetString());
		Assert.Equal("integer", summary.GetProperty("categoriesCount").GetProperty("type").GetString());

		Assert.Equal("#/components/schemas/PublicCategoryGroup", map.GetProperty("categoryGroups").GetProperty("items").GetProperty("$ref").GetString());
		Assert.Equal("#/components/schemas/PublicMarker", map.GetProperty("markers").GetProperty("items").GetProperty("$ref").GetString());
		var style = schemas.GetProperty("PublicMapStyle").GetProperty("properties");

		Assert.False(style.TryGetProperty("progress", out _));
		Assert.False(style.TryGetProperty("error", out _));
		Assert.False(style.TryGetProperty("source", out _));
		Assert.True(schemas.GetProperty("Game").GetProperty("properties").TryGetProperty("status", out _));
		Assert.True(paths.GetProperty("/games/{id}").TryGetProperty("get", out _));
	}

	[Fact]
	public async Task ReadyStylesPreviewsAndMixedMediaAreReturnedWithoutOperationalFields()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "styled", PublicationStatus.Published);
		await using var scope = factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var image = await db.MediaAssets.OfType<ImageAsset>().AsTracking().SingleAsync(asset => asset.Id == MediaTestData.ImageId);
		var map = Map.Create(game.Id, "World", Slug.Create("world").Value!, PublicationStatus.Published).Value!;
		var first = map.AddStyle(new MapStyleInput("First", "#123456", image)).Value!;
		var unfinished = map.AddStyle(new MapStyleInput("Pending", "#654321", image)).Value!;
		var second = map.AddStyle(new MapStyleInput("Second", "#abcdef", image)).Value!;
		Assert.True(first.Claim(first.AttemptId).IsSuccess);
		Assert.True(first.Publish(first.AttemptId).IsSuccess);
		Assert.True(second.Claim(second.AttemptId).IsSuccess);
		Assert.True(second.Publish(second.AttemptId).IsSuccess);
		map.ReconcileDefaultStyle();
		db.Maps.Add(map);
		var group = CategoryGroup.Create(map.Id, "Group", 1, image.Id, status: PublicationStatus.Published).Value!;
		var category = Category.Create(group.Id, "Category", image.Id, "#123456", 1, PublicationStatus.Published).Value!;
		db.CategoryGroups.Add(group);
		db.Categories.Add(category);
		db.Markers.Add(Marker.Create(category.Id, "Media", Position.Create(0, 0).Value!,
			media: new Guid?[] { MediaTestData.VideoId, image.Id }, status: PublicationStatus.Published).Value!);
		await db.SaveChangesAsync();
		using var client = factory.CreateHttpsClient();
		var detail = (await client.GetFromJsonAsync<PublicGameDto>("/public/games/styled"))!;

		Assert.Equal(image.Id, Assert.Single(detail.Maps).Preview!.Id);
		var result = (await client.GetFromJsonAsync<PublicMapDetailsDto>("/public/games/styled/maps/world"))!;

		Assert.Equal(new[] { first.Id, second.Id }, result.Styles.Select(style => style.Id));
		Assert.DoesNotContain(result.Styles, style => style.Id == unfinished.Id);
		Assert.Equal(first.Id, result.DefaultStyleId);
		Assert.All(result.Styles, style => Assert.Contains("/{z}/{x}/{y}.webp", style.TilesUrlTemplate));
		Assert.Equal(image.Id, Assert.Single(result.CategoryGroups).Icon!.Id);
		Assert.Equal(new[] { MediaTestData.VideoId, image.Id }, Assert.Single(result.Markers).Media.Select(asset => asset.Id));
		var json = await client.GetFromJsonAsync<JsonElement>("/public/games/styled/maps/world");

		Assert.Equal("video", json.GetProperty("markers")[0].GetProperty("media")[0].GetProperty("type").GetString());
		Assert.Equal("image", json.GetProperty("markers")[0].GetProperty("media")[0].GetProperty("preview").GetProperty("type").GetString());
		Assert.False(json.GetProperty("styles")[0].TryGetProperty("status", out _));
		Assert.False(json.GetProperty("markers")[0].TryGetProperty("status", out _));
	}
}
