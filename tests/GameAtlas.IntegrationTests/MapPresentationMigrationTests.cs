using GameAtlas.Domain.Categories;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Markers;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapPresentationMigrationTests
{
	[Fact]
	public async Task PopulatedMigrationPreservesGeometryContentFilesOrderAndReadyDefault()
	{
		await using var factory = new GameApiFactory();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		await context.GetService<IMigrator>().MigrateAsync("20261030211320_ExpandedGameMetadata");
		await MediaTestData.EnsureCoverAsync(context);
		var game = Game.Create("Game", "migration", MediaTestData.CoverId).Value!;
		context.Games.Add(game);
		await context.SaveChangesAsync();
		var mapId = Guid.CreateVersion7();
		var blankId = Guid.CreateVersion7();
		var first = Guid.CreateVersion7();
		var second = Guid.CreateVersion7();
		var now = DateTimeOffset.UtcNow;

		await context.Database.ExecuteSqlInterpolatedAsync($"""
			INSERT INTO Maps (Id, GameId, Name, NormalizedName, Width, Height, LastCreationSequence, CreatedAt, UpdatedAt)
			VALUES ({mapId}, {game.Id}, 'World', 'WORLD', 1025, 513, 2, {now}, {now}),
			       ({blankId}, {game.Id}, 'Blank', 'BLANK', 23, 17, 0, {now}, {now});
			INSERT INTO MapStyles (Id, MapId, Name, NormalizedName, BackgroundColor, CreationSequence,
			 SourceAssetId, Status, AttemptId, CreatedAt, UpdatedAt)
			VALUES ({first}, {mapId}, 'First', 'FIRST', '#FFFFFF', 1, {MediaTestData.CoverId}, 'Queued', {Guid.CreateVersion7()}, {now}, {now}),
			       ({second}, {mapId}, 'Second', 'SECOND', '#FFFFFF', 2, {MediaTestData.CoverId}, 'Ready', {Guid.CreateVersion7()}, {now}, {now});
			""");
		var group = CategoryGroup.Create(mapId, "Group", 1).Value!;
		var category = Category.Create(group.Id, "Category", MediaTestData.CoverId, "#fff", 1).Value!;
		var marker = Marker.Create(category.Id, "Point", Position.Create(-12.5, 64.25).Value!).Value!;
		context.CategoryGroups.Add(group);
		context.Categories.Add(category);
		context.Markers.Add(marker);
		await context.SaveChangesAsync();
		var tilePath = Path.Combine(factory.MapStoragePath, "tiles", mapId.ToString("N"), second.ToString("N"), "0", "0", "0.webp");
		Directory.CreateDirectory(Path.GetDirectoryName(tilePath)!);
		await File.WriteAllBytesAsync(tilePath, [1, 2, 3]);
		await context.Database.MigrateAsync();
		context.ChangeTracker.Clear();
		var map = await context.Maps.Include(item => item.Styles).SingleAsync(item => item.Id == mapId);
		var blank = await context.Maps.SingleAsync(item => item.Id == blankId);
		var point = await context.Markers.SingleAsync();

		Assert.Equal(1025, map.Width);
		Assert.Equal(513, map.Height);
		Assert.Equal(23, blank.Width);
		Assert.Equal(second, map.DefaultStyleId);
		Assert.Equal(new[] { first, second }, map.Styles.OrderBy(style => style.DisplayOrder).Select(style => style.Id));
		Assert.All(map.Styles, style => Assert.Equal(MediaTestData.CoverId, style.SourceAssetId));
		Assert.Equal(marker.Position, point.Position);
		Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(tilePath));
	}
}
