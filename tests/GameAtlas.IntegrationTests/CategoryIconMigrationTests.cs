using GameAtlas.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class CategoryIconMigrationTests
{
	[Fact]
	public async Task LegacyIconsRefuseUpgradeWithoutChangingExistingData()
	{
		await using var factory = new GameApiFactory();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var migrator = context.GetService<IMigrator>();

		await migrator.MigrateAsync("20261024135813_MediaLibrary");
		var gameId = await MediaTestData.SeedLegacyGameAsync(context);
		var map = GameAtlas.Domain.Maps.Map.Create(gameId, "World").Value!;
		var now = DateTimeOffset.UtcNow;
		await context.Database.ExecuteSqlInterpolatedAsync($"""
 INSERT INTO Maps (Id, GameId, Name, NormalizedName, Width, Height, LastCreationSequence, CreatedAt, UpdatedAt)
 VALUES ({map.Id}, {gameId}, 'World', 'WORLD', 1, 1, 0, {now}, {now});
 """);
		var group = GameAtlas.Domain.CategoryGroups.CategoryGroup.Create(map.Id, "Group", 1).Value!;
		context.CategoryGroups.Add(group);
		await context.SaveChangesAsync();
		var id = Guid.CreateVersion7();
		var created = new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);

		await context.Database.ExecuteSqlAsync($"""
			INSERT INTO "Categories" ("Id", "CategoryGroupId", "Name", "NormalizedName", "Icon", "Color",
			    "Order", "CreatedAt", "UpdatedAt")
			VALUES ({id}, {group.Id}, 'Legacy', 'LEGACY', '/legacy/icon.svg', '#FFFFFF', 4, {created}, {created});
			""");

		var refusal = await Assert.ThrowsAsync<SqliteException>(() => migrator.MigrateAsync());

		Assert.Contains("Media_RequiresEmptyLegacyCategories", refusal.Message);
		var original = await context.Database.SqlQuery<string>($"SELECT \"Icon\" AS \"Value\" FROM \"Categories\"")
			.SingleAsync();

		Assert.Equal("/legacy/icon.svg", original);
		Assert.Equal(gameId, await context.Database.SqlQuery<Guid>($"SELECT Id AS Value FROM Games").SingleAsync());
	}
}
