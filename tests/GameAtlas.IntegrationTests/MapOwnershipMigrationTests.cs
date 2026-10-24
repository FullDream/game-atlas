using GameAtlas.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapOwnershipMigrationTests
{
	[Fact]
	public async Task PopulatedLegacyOwnershipUpgradeIsRefusedWithoutDiscardingRows()
	{
		await using var factory = new GameApiFactory();
		await SeedLegacyAsync(factory);

		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var refused = await Assert.ThrowsAsync<SqliteException>(() => context.Database.MigrateAsync());

		Assert.Contains("MapOwnership_RequiresExplicitPreparation", refused.Message);
		Assert.Equal(1, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM CategoryGroups WHERE GameId IS NOT NULL").SingleAsync());
		Assert.Equal(1, await context.Markers.CountAsync());
		Assert.Equal(1, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM MarkerMedia").SingleAsync());
	}

	private static async Task SeedLegacyAsync(GameApiFactory factory)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		await context.GetService<IMigrator>().MigrateAsync("20261023210838_SimplifyMapStyles");
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "legacy");
		var group = Guid.CreateVersion7();
		var category = Guid.CreateVersion7();
		var marker = Guid.CreateVersion7();
		var media = Guid.CreateVersion7();
		var now = DateTimeOffset.UtcNow;

		await context.Database.ExecuteSqlInterpolatedAsync($"""
			INSERT INTO CategoryGroups (Id, GameId, Name, NormalizedName, "Order", CreatedAt, UpdatedAt)
			VALUES ({group}, {game.Id}, 'Resources', 'RESOURCES', 3, {now}, {now});
			INSERT INTO Categories (Id, CategoryGroupId, Name, NormalizedName, Icon, Color, "Order", CreatedAt, UpdatedAt)
			VALUES ({category}, {group}, 'Chest', 'CHEST', 'icon', '#FFFFFF', 2, {now}, {now});
			INSERT INTO Markers (Id, CategoryId, Name, Description, PositionX, PositionY, CreatedAt, UpdatedAt)
			VALUES ({marker}, {category}, 'Point', 'description', -12.5, 64.25, {now}, {now});
			INSERT INTO MarkerMedia (Id, MarkerId, "Order", Type, Url, Width, Height, Alt)
			VALUES ({media}, {marker}, 0, 'image', 'picture.png', 640, 480, '');
			""");
	}
}
