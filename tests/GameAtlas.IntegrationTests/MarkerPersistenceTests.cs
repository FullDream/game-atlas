using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;
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

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		await factory.MigrateAsync();
		var position = Assert.IsType<Position>(Position.Create(123.45, -67.89).Value);
		var marker = Assert.IsType<Marker>(Marker.Create(category.Id, "Chest", position, "Description",
			[
				Assert.IsType<ImageMedia>(ImageMedia.Create("image", 1920, 1080, "Alt").Value),
				Assert.IsType<YouTubeVideoMedia>(YouTubeVideoMedia.Create("dQw4w9WgXcQ").Value),
				Assert.IsType<VideoMedia>(VideoMedia.Create("video", "preview").Value)
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
		Assert.Equal("Alt", Assert.IsType<ImageMedia>(media[0]).Alt);
		Assert.Equal("dQw4w9WgXcQ", Assert.IsType<YouTubeVideoMedia>(media[1]).VideoId);
		Assert.Equal("preview", Assert.IsType<VideoMedia>(media[2]).Preview);
		Assert.All(media, item => Assert.Equal(stored.Id, item.MarkerId));
		Assert.Equal(marker.Media.Select(item => item.Id), media.Select(item => item.Id));
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
		var orphanMedia = Assert.IsType<VideoMedia>(VideoMedia.Create("orphan-video").Value);

		reloaded.Set<MarkerMedia>().Add(orphanMedia);
		reloaded.Entry(orphanMedia).Property(item => item.MarkerId).CurrentValue = Guid.NewGuid();
		await Assert.ThrowsAsync<DbUpdateException>(() => reloaded.SaveChangesAsync());
		reloaded.ChangeTracker.Clear();
		await reloaded.GetService<IMigrator>().MigrateAsync("20261023114301_Categories");

		Assert.Equal(game.Id, (await reloaded.Games.SingleAsync()).Id);
		Assert.Equal(group.Id, (await reloaded.CategoryGroups.SingleAsync()).Id);
		Assert.Equal(category.Id, (await reloaded.Categories.SingleAsync()).Id);
		Assert.Empty(await reloaded.Database.SqlQueryRaw<string>(
			"SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN ('Markers', 'MarkerMedia', 'Position')")
			.ToArrayAsync());
	}
}
