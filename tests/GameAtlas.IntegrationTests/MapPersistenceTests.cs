using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapPersistenceTests
{
	[Fact]
	public async Task SchemaUpgradeRefusesExistingMapsBeforeChangingLegacyData()
	{
		await using var factory = new GameApiFactory();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		await context.GetService<IMigrator>().MigrateAsync("20261023181923_Maps");
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "legacy-map");
		var mapId = Guid.CreateVersion7();
		var styleId = Guid.CreateVersion7();
		var sourceId = Guid.CreateVersion7();
		var attemptId = Guid.CreateVersion7();
		var now = DateTimeOffset.UtcNow;

		await context.Database.ExecuteSqlInterpolatedAsync($"""
			INSERT INTO "Maps" ("Id", "GameId", "Width", "Height", "LastCreationSequence", "CreatedAt", "UpdatedAt")
			VALUES ({mapId}, {game.Id}, 23, 17, 1, {now}, {now});
			INSERT INTO "MapVariants" ("Id", "MapId", "Name", "NormalizedName", "BackgroundColor",
				"CreationSequence", "SourceVersionId", "ActiveVersionId", "CreatedAt", "UpdatedAt")
			VALUES ({styleId}, {mapId}, 'Atlas', 'ATLAS', '#FFFFFF', 1, {sourceId}, {sourceId}, {now}, {now});
			INSERT INTO "MapSourceVersions" ("Id", "MapVariantId", "Format", "Width", "Height", "ByteLength",
				"Status", "Progress", "AttemptId", "CreatedAt", "UpdatedAt")
			VALUES ({sourceId}, {styleId}, 'png', 23, 17, 100, 'Ready', 100, {attemptId}, {now}, {now});
			""");

		var error = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
			context.Database.MigrateAsync());

		Assert.Contains("MapStyleUpgrade_RequiresEmptyLegacyMapData", error.Message);
		Assert.Equal(1, await context.Maps.CountAsync());
		Assert.Equal(1, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM MapVariants").SingleAsync());
		Assert.Equal(1, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM MapSourceVersions WHERE Status = 'Ready' AND Progress = 100").SingleAsync());
		Assert.DoesNotContain(await context.Database.GetAppliedMigrationsAsync(),
			migration => migration.EndsWith("_SimplifyMapStyles"));
		DeleteGameHandlerTests.AssertSameGame(game, await context.Games.SingleAsync());
	}

	[Fact]
	public async Task UpgradeReconcilesLegacyIdsAndRollbackPreservesAllActualParentAndMarkerData()
	{
		await using var factory = new GameApiFactory();

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await context.GetService<IMigrator>().MigrateAsync("20261023145510_Markers");
		}

		var game = await UpdateGameHandlerTests.SeedAsync(factory, "legacy-id");

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var legacyMapId = Guid.NewGuid();

			await context.Database.ExecuteSqlInterpolatedAsync(
				$"UPDATE Games SET MapId = {legacyMapId} WHERE Id = {game.Id}");
		}

		await factory.MigrateAsync();
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: System.Text.Json.JsonDocument.Parse(MarkerTestData.MixedMedia).RootElement.Clone());
		var markerId = marker.GetProperty("id").GetGuid();
		var map = await MapTestData.SeedAsync(factory, game.Id);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var stored = await context.Maps.Include(map => map.Styles)
				.SingleAsync();

			Assert.Equal(map.Id, stored.Id);
			Assert.Equal(map.Geometry, stored.Geometry);
			Assert.Equal(map.Styles.Single().Id, stored.Styles.Single().Id);
			Assert.Equal(MapProcessingStatus.Queued, stored.Styles.Single().Status);
			Assert.False(context.Database.HasPendingModelChanges());
			var duplicate = Map.Create(game.Id, map.Name, 1, 1).Value!;

			context.Maps.Add(duplicate);
			var conflict = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

			Assert.True(new SqlitePersistenceFailureClassifier().IsMapNameConflict(conflict));
		}

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			var rollback = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
				context.GetService<IMigrator>().MigrateAsync("20261023145510_Markers"));

			Assert.Contains("MapOwnership_RestoreBackupForNonemptyMaps", rollback.Message);
			DeleteGameHandlerTests.AssertSameGame(game, await context.Games.SingleAsync());
			Assert.Equal(group.Id, (await context.CategoryGroups.SingleAsync()).Id);
			Assert.Equal(category.Id, (await context.Categories.SingleAsync()).Id);
			var storedMarker = await context.Markers.Include(marker => marker.Media).SingleAsync();

			Assert.Equal(markerId, storedMarker.Id);
			Assert.Equal(3, storedMarker.Media.Count);
			Assert.Equal(map.Id, (await context.Maps.SingleAsync()).Id);
		}
	}
}
