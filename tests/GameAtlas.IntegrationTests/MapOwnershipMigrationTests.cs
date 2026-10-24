using GameAtlas.Application.Maps.Storage;
using GameAtlas.Infrastructure.Maps.Persistence;
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
	public async Task PreparedOwnershipPreservesIdsCoordinatesMediaAndTimestamps()
	{
		await using var factory = new GameApiFactory();
		var legacy = await SeedLegacyAsync(factory);
		var mapId = Guid.CreateVersion7();
		var prepared = await MapOwnershipPreparation.PrepareAsync(factory.DatabasePath,
			new MapOwnershipInput([new(mapId, legacy.GameId, " Мир ", 1025, 513)], [new(legacy.GroupId, mapId)]));

		Assert.True(prepared.IsSuccess);
		await factory.MigrateAsync();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var map = await context.Maps.SingleAsync();
		var group = await context.CategoryGroups.SingleAsync();
		var category = await context.Categories.SingleAsync();
		var marker = await context.Markers.Include(item => item.Media).SingleAsync();

		Assert.Equal(mapId, map.Id);
		Assert.Equal("Мир", map.Name);
		Assert.Equal("МИР", map.NormalizedName);
		Assert.Equal(1025, map.Width);
		Assert.Equal(513, map.Height);
		Assert.Equal(legacy.GroupId, group.Id);
		Assert.Equal(mapId, group.MapId);
		Assert.Equal(legacy.Time, group.CreatedAt);
		Assert.Equal(legacy.Time, group.UpdatedAt);
		Assert.Equal(legacy.CategoryId, category.Id);
		Assert.Equal(legacy.MarkerId, marker.Id);
		Assert.Equal(-12.5, marker.Position.X);
		Assert.Equal(64.25, marker.Position.Y);
		Assert.Equal(legacy.Time, marker.UpdatedAt);
		Assert.Equal(legacy.MediaId, Assert.Single(marker.Media).Id);
		Assert.Equal(0, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").SingleAsync());
		Assert.False(context.Database.HasPendingModelChanges());
	}

	[Fact]
	public async Task ExistingMapAndStyleFilesKeepIdentityWhenAnotherMapReceivesLegacyGroups()
	{
		await using var factory = new GameApiFactory();
		var legacy = await SeedLegacyAsync(factory);
		var oldMap = Guid.CreateVersion7();
		var newMap = Guid.CreateVersion7();
		var style = Guid.CreateVersion7();
		var bytes = MapTestData.ImageBytes();
		var storage = factory.Services.GetRequiredService<IMapStorage>();
		var original = storage.OriginalPath(oldMap, style);

		Directory.CreateDirectory(Path.GetDirectoryName(original)!);
		await File.WriteAllBytesAsync(original, bytes);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await context.Database.ExecuteSqlInterpolatedAsync($"""
				INSERT INTO Maps (Id, GameId, Width, Height, LastCreationSequence, CreatedAt, UpdatedAt)
				VALUES ({oldMap}, {legacy.GameId}, 23, 17, 1, {legacy.Time}, {legacy.Time});
				INSERT INTO MapStyles (Id, MapId, Name, NormalizedName, BackgroundColor, CreationSequence,
					Format, Width, Height, ByteLength, Status, Progress, AttemptId, CreatedAt, UpdatedAt)
				VALUES ({style}, {oldMap}, 'Atlas', 'ATLAS', '#FFFFFF', 1,
					'png', 23, 17, {bytes.Length}, 'Ready', 100, {Guid.CreateVersion7()}, {legacy.Time}, {legacy.Time});
				""");
		}

		var input = new MapOwnershipInput([
			new(oldMap, legacy.GameId, "Legacy", 23, 17), new(newMap, legacy.GameId, "World", 1025, 513)
		], [new(legacy.GroupId, newMap)]);
		var prepared = await MapOwnershipPreparation.PrepareAsync(factory.DatabasePath, input);

		Assert.True(prepared.IsSuccess);
		await factory.MigrateAsync();
		await using var reload = factory.Services.CreateAsyncScope();
		var database = reload.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var stored = await database.Maps.Include(map => map.Styles).SingleAsync(map => map.Id == oldMap);

		Assert.Equal("Legacy", stored.Name);
		Assert.Equal(legacy.Time, stored.CreatedAt);
		Assert.Equal(legacy.Time, stored.UpdatedAt);
		Assert.Equal(style, Assert.Single(stored.Styles).Id);
		Assert.Equal(newMap, (await database.CategoryGroups.SingleAsync()).MapId);
		Assert.Equal(2, await database.Maps.CountAsync());
		Assert.Equal(bytes, await File.ReadAllBytesAsync(original));
		var refused = await Assert.ThrowsAsync<SqliteException>(() =>
			database.GetService<IMigrator>().MigrateAsync("20261023210838_SimplifyMapStyles"));

		Assert.Contains("MapOwnership_RestoreBackupForNonemptyMaps", refused.Message);
		Assert.Equal(legacy.MarkerId, (await database.Markers.SingleAsync()).Id);
	}

	[Fact]
	public async Task UnpreparedMigrationAndAmbiguousPreparationPreserveLegacyRows()
	{
		await using var factory = new GameApiFactory();
		var legacy = await SeedLegacyAsync(factory);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var refused = await Assert.ThrowsAsync<SqliteException>(() => context.Database.MigrateAsync());

		Assert.Contains("MapOwnership_RequiresExplicitPreparation", refused.Message);
		var first = Guid.CreateVersion7();
		var second = Guid.CreateVersion7();
		var duplicate = await MapOwnershipPreparation.PrepareAsync(factory.DatabasePath,
			new MapOwnershipInput([new(first, legacy.GameId, "World", 1, 1),
				new(second, legacy.GameId, " world ", 1, 1)], [new(legacy.GroupId, first)]));
		var missing = await MapOwnershipPreparation.PrepareAsync(factory.DatabasePath,
			new MapOwnershipInput([], []));

		Assert.True(duplicate.IsFailure);
		Assert.True(missing.IsFailure);
		Assert.Equal(1, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM CategoryGroups WHERE GameId IS NOT NULL").SingleAsync());
		Assert.Equal(1, await context.Markers.CountAsync());
		Assert.Equal(1, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM MarkerMedia").SingleAsync());
	}

	private static async Task<(Guid GameId, Guid GroupId, Guid CategoryId, Guid MarkerId, Guid MediaId,
		DateTimeOffset Time)> SeedLegacyAsync(GameApiFactory factory)
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

		return (game.Id, group, category, marker, media, now);
	}
}
