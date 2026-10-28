using GameAtlas.Domain.Markers;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MarkerPersistenceTests
{
	[Fact]
	public async Task UpgradeRoundTripsImmutablePositionAndAllMediaThenRollbackPreservesParents()
	{
		await using var factory = new GameApiFactory();

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
				.GetService<IMigrator>().MigrateAsync("20261023114301_Categories");
		}

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		var position = Assert.IsType<Position>(Position.Create(123.45, -67.89).Value);
		var marker = Assert.IsType<Marker>(Marker.Create(category.Id, "Chest", position, "Description",
			[
				MediaTestData.ImageId, MediaTestData.PreviewId, MediaTestData.VideoId
			]).Value);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			context.Markers.Add(marker);
			await context.SaveChangesAsync();
		}

		await using var reload = factory.Services.CreateAsyncScope();
		var reloaded = reload.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var stored = await reloaded.Markers.Include(item => item.Media).SingleAsync();
		var media = stored.Media.OrderBy(item => item.Order).ToArray();

		Assert.Equal(position, stored.Position);
		Assert.Equal(marker.Id, stored.Id);
		Assert.Equal(marker.CategoryId, stored.CategoryId);
		Assert.Equal(marker.CreatedAt, stored.CreatedAt);
		Assert.Equal(marker.UpdatedAt, stored.UpdatedAt);
		Assert.Equal(new[] { MediaTestData.ImageId, MediaTestData.PreviewId, MediaTestData.VideoId },
			media.Select(item => item.AssetId));
		Assert.All(media, item => Assert.Equal(stored.Id, item.MarkerId));
		Assert.Equal(marker.Media.Select(item => item.AssetId), media.Select(item => item.AssetId));
		Assert.Null(reloaded.Model.FindEntityType(typeof(Position)));
		Assert.Null(reloaded.Model.FindEntityType(typeof(GameAtlas.Domain.Categories.Category))
			?.FindProperty("MarkersCount"));
		Assert.False(reloaded.Database.HasPendingModelChanges());
		reloaded.Markers.Add(Assert.IsType<Marker>(Marker.Create(Guid.NewGuid(), "Orphan", position).Value));
		var failure = await Assert.ThrowsAsync<DbUpdateException>(() => reloaded.SaveChangesAsync());
		var classifier = new SqlitePersistenceFailureClassifier();

		Assert.False(classifier.IsCategoryNameConflict(failure));
		Assert.False(classifier.IsGameSlugConflict(failure));
		reloaded.ChangeTracker.Clear();
		var orphan = Marker.Create(Guid.NewGuid(), "Orphan", position, media: [MediaTestData.VideoId]).Value!;
		var orphanMedia = Assert.Single(orphan.Media);

		reloaded.Set<MarkerAsset>().Add(orphanMedia);
		await Assert.ThrowsAsync<DbUpdateException>(() => reloaded.SaveChangesAsync());
		reloaded.ChangeTracker.Clear();
		var rollback = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
			reloaded.GetService<IMigrator>().MigrateAsync("20261023114301_Categories"));

		Assert.Contains("GameImages_RestoreBackupForNonemptyGames", rollback.Message);

		Assert.Equal(game.Id, (await reloaded.Games.SingleAsync()).Id);
		Assert.Equal(group.Id, (await reloaded.CategoryGroups.SingleAsync()).Id);
		Assert.Equal(category.Id, (await reloaded.Categories.SingleAsync()).Id);
		Assert.Equal(marker.Id, (await reloaded.Markers.SingleAsync()).Id);
		Assert.Equal(3, await reloaded.Set<MarkerAsset>().CountAsync());
	}
}
