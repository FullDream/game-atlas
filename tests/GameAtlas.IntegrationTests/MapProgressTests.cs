using GameAtlas.Application.Maps;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapProgressTests
{
	[Fact]
	public void PercentagesAreTransientMonotonicAndCannotLeakBetweenAttempts()
	{
		var image = ImageAsset.Create(Guid.CreateVersion7(), "Source", null, "image/png", 100, 1, 1).Value;
		var map = Map.Create(Guid.CreateVersion7(), "Map").Value!;
		map.AddStyle(new MapStyleInput("Style", "#fff", image));
		var style = map.Styles.Single();
		var store = new MapProgressStore();
		var attempt = style.AttemptId;

		Assert.Equal(0, store.Read(style));
		Assert.True(style.Claim(attempt).IsSuccess);
		var timestamp = style.UpdatedAt;
		var progress = store.Start(style.Id, attempt);

		progress.Report(60);
		progress.Report(-1);
		Assert.Equal(60, style.ToDto(store).Progress);
		progress.Report(200);
		Assert.Equal(99, store.Read(style));
		Assert.Equal(timestamp, style.UpdatedAt);
		Assert.Equal(0, new MapProgressStore().Read(style));
		Assert.True(style.Fail(attempt).IsSuccess);
		Assert.Equal(99, store.Read(style));
		Assert.True(style.Retry().IsSuccess);
		Assert.Equal(0, store.Read(style));
		Assert.True(style.Claim(style.AttemptId).IsSuccess);
		var next = store.Start(style.Id, style.AttemptId);

		progress.Report(99);
		next.Report(15);
		Assert.Equal(15, store.Read(style));
		Assert.True(style.Publish(attempt).IsFailure);
		Assert.True(style.Publish(style.AttemptId).IsSuccess);
		Assert.Equal(100, new MapProgressStore().Read(style));
	}

	[Fact]
	public async Task UpgradeDropsOnlyStoredPercentagesAndPreservesReadyStyles()
	{
		await using var factory = new GameApiFactory();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var migrator = context.GetService<IMigrator>();
		await migrator.MigrateAsync("20261028202100_GameImageAssets");
		await MediaTestData.EnsureCoverAsync(context);
		var gameId = Guid.CreateVersion7();
		var mapId = Guid.CreateVersion7();
		var styleId = Guid.CreateVersion7();
		var sourceId = MediaTestData.CoverId;
		var timestamp = DateTimeOffset.UtcNow;
		var storedTimestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", System.Globalization.CultureInfo.InvariantCulture);
		await context.Database.ExecuteSqlInterpolatedAsync($"""
 INSERT INTO Games (Id, Name, Slug, ImageAssetId, CreatedAt, UpdatedAt, Status)
 VALUES ({gameId}, 'Game', 'migration', {sourceId}, {storedTimestamp}, {storedTimestamp}, 'Draft');
 INSERT INTO Maps (Id, GameId, Name, NormalizedName, Width, Height, LastCreationSequence, CreatedAt, UpdatedAt)
 VALUES ({mapId}, {gameId}, 'World', 'WORLD', 64, 48, 1, {storedTimestamp}, {storedTimestamp});
 INSERT INTO MapStyles (Id, MapId, Name, NormalizedName, BackgroundColor, CreationSequence,
 SourceAssetId, Status, Progress, AttemptId, CreatedAt, UpdatedAt)
 VALUES ({styleId}, {mapId}, 'Atlas', 'ATLAS', '#FFFFFF', 1, {sourceId}, 'Ready', 100, {Guid.CreateVersion7()}, {storedTimestamp}, {storedTimestamp});
 """);
		await migrator.MigrateAsync();
		context.ChangeTracker.Clear();
		var restored = await context.MapStyles.SingleAsync();

		Assert.Equal(styleId, restored.Id);
		Assert.Equal(mapId, restored.MapId);
		Assert.Equal(sourceId, restored.SourceAssetId);
		Assert.Equal(timestamp, restored.UpdatedAt);
		Assert.Equal(MapProcessingStatus.Ready, restored.Status);
		Assert.DoesNotContain(context.Model.FindEntityType(typeof(MapStyle))!.GetProperties(),
			property => property.Name == "Progress");
	}
}
