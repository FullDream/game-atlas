using GameAtlas.Application.Common.Pagination;
using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Games;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Markers;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class GameMetadataApiTests
{
	[Fact]
	public async Task SavesClearsAndProtectsBannerAndPlainTextMetadata()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var cover = await MediaTestData.UploadImageAsync(client);
		var banner = await MediaTestData.UploadImageAsync(client);
		using var created = await client.PostAsJsonAsync("/games", new
		{
			name = "Game",
			slug = "game",
			coverId = cover.Id,
			bannerId = banner.Id,
			releaseDate = "2026-10-31",
			developer = " Studio ",
			description = "First\n<b>Second</b>"
		});
		created.EnsureSuccessStatusCode();
		var game = (await created.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!;
		Assert.Equal(new DateOnly(2026, 10, 31), game.ReleaseDate);
		Assert.Equal("Studio", game.Developer);
		Assert.Equal("First\n<b>Second</b>", game.Description);
		Assert.Equal(banner, game.Banner);
		using var blocked = await client.DeleteAsync($"/media/assets/{banner.Id}");
		Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
		using var invalid = await client.PatchAsJsonAsync($"/games/{game.Id}", new { name = "Changed", releaseDate = "2026-02-30" });
		Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
		using var legacy = await client.PatchAsJsonAsync($"/games/{game.Id}", new { imageAssetId = cover.Id });
		Assert.Equal(HttpStatusCode.BadRequest, legacy.StatusCode);
		using var cleared = await client.PatchAsJsonAsync($"/games/{game.Id}", new { bannerId = (Guid?)null, releaseDate = (string?)null, developer = " ", description = "" });
		cleared.EnsureSuccessStatusCode();
		var actual = (await client.GetFromJsonAsync<GameDto>($"/games/{game.Id}", MapTestData.JsonOptions))!;
		Assert.Equal("Game", actual.Name);
		Assert.Null(actual.Banner);
		Assert.Null(actual.ReleaseDate);
		Assert.Null(actual.Developer);
		Assert.Null(actual.Description);
		Assert.Equal(cover, actual.Cover);
		using var removed = await client.DeleteAsync($"/media/assets/{banner.Id}");
		Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
	}

	[Fact]
	public async Task CountsOwnedContentAndReturnsOnlyLightweightMaps()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await MediaTestData.EnsureCoverAsync(context);
		var game = Game.Create("Game", "game", MediaTestData.CoverId).Value!;
		var other = Game.Create("Other", "other", MediaTestData.CoverId).Value!;
		context.Games.AddRange(game, other);
		var map = Map.Create(game.Id, "World", 64, 48).Value!;
		var blank = Map.Create(game.Id, "Blank", 64, 48).Value!;
		var foreign = Map.Create(other.Id, "Foreign", 64, 48).Value!;
		context.Maps.AddRange(map, blank, foreign);
		var group = CategoryGroup.Create(map.Id, "Group", 1).Value!;
		var otherGroup = CategoryGroup.Create(foreign.Id, "Group", 1).Value!;
		context.CategoryGroups.AddRange(group, otherGroup);
		var category = Category.Create(group.Id, "Category", MediaTestData.CoverId, "#fff", 1).Value!;
		context.Categories.AddRange(category, Category.Create(group.Id, "Empty", MediaTestData.CoverId, "#fff", 2).Value!,
			Category.Create(otherGroup.Id, "Foreign", MediaTestData.CoverId, "#fff", 1).Value!);
		context.Markers.Add(Marker.Create(category.Id, "Point", Position.Create(0, 0).Value!).Value!);
		await context.SaveChangesAsync();
		using var client = factory.CreateHttpsClient();
		var actual = (await client.GetFromJsonAsync<GameDto>($"/games/{game.Id}", MapTestData.JsonOptions))!;
		Assert.Equal(2, actual.CategoriesCount);
		Assert.Equal(1, actual.MarkersCount);
		var page = (await client.GetFromJsonAsync<PaginatedResult<GameDto>>("/games", MapTestData.JsonOptions))!;
		var listed = Assert.Single(page.Items, item => item.Id == game.Id);
		Assert.Equal(actual.CategoriesCount, listed.CategoriesCount);
		Assert.Equal(actual.MarkersCount, listed.MarkersCount);
		Assert.Equal(new[] { map.Id, blank.Id }, actual.Maps.Select(item => item.Id));
		Assert.All(actual.Maps, item => Assert.Null(item.Preview));
		Assert.Equal(game.UpdatedAt, actual.UpdatedAt);
		var image = await context.MediaAssets.OfType<GameAtlas.Domain.Media.ImageAsset>().AsTracking().SingleAsync();
		var first = map.AddStyle(new MapStyleInput("First", "#fff", image)).Value!;
		var otherImage = GameAtlas.Domain.Media.ImageAsset.Create(Guid.CreateVersion7(), "Second source", null, "image/png", 64, 64, 48).Value!;
		context.MediaAssets.Add(otherImage);
		var second = map.AddStyle(new MapStyleInput("Second", "#fff", otherImage)).Value!;
		second.Claim(second.AttemptId);
		second.Publish(second.AttemptId);
		await context.SaveChangesAsync();
		var ready = (await client.GetFromJsonAsync<GameDto>($"/games/{game.Id}", MapTestData.JsonOptions))!;
		Assert.Equal(otherImage.Id, ready.Maps[0].Preview!.Id);
		first.Claim(first.AttemptId);
		first.Publish(first.AttemptId);
		map.DeleteStyle(second.Id);
		await context.SaveChangesAsync();
		var earlier = (await client.GetFromJsonAsync<GameDto>($"/games/{game.Id}", MapTestData.JsonOptions))!;
		Assert.Equal(image.Id, earlier.Maps[0].Preview!.Id);
		map.DeleteStyle(first.Id);
		await context.SaveChangesAsync();
		var empty = (await client.GetFromJsonAsync<GameDto>($"/games/{game.Id}", MapTestData.JsonOptions))!;
		Assert.Null(empty.Maps[0].Preview);
	}

	[Fact]
	public async Task UpgradePreservesExistingGameAndMaps()
	{
		await using var factory = new GameApiFactory();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var previous = context.Database.GetMigrations().Last(name => name.EndsWith("GameImageAssets"));
		await context.GetService<IMigrator>().MigrateAsync(previous);
		await MediaTestData.EnsureCoverAsync(context);
		var id = Guid.CreateVersion7();
		var timestamp = DateTimeOffset.UtcNow;
		var storedTimestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", System.Globalization.CultureInfo.InvariantCulture);
		await context.Database.ExecuteSqlAsync($"INSERT INTO Games (Id, Name, Slug, ImageAssetId, CreatedAt, UpdatedAt, Status) VALUES ({id}, 'Game', 'game', {MediaTestData.CoverId}, {storedTimestamp}, {storedTimestamp}, 'Draft')");
		var map = Map.Create(id, "World", 64, 48).Value!;
		context.Maps.Add(map);
		await context.SaveChangesAsync();
		await context.Database.MigrateAsync();
		context.ChangeTracker.Clear();
		var game = await context.Games.SingleAsync();
		Assert.Equal(MediaTestData.CoverId, game.CoverId);
		Assert.Equal(timestamp, game.UpdatedAt);
		Assert.Null(game.BannerId);
		Assert.Null(game.Developer);
		Assert.Equal(map.Id, (await context.Maps.SingleAsync()).Id);
	}
}
