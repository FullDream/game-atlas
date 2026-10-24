using GameAtlas.Application.Maps.Storage;
using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapAdmissionRecoveryTests
{
	[Fact]
	public async Task ExpiredPromotionsAreReconciledWhileLiveAndCommittedSourcesRemainUntouched()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await using var scope = factory.Services.CreateAsyncScope();
		var sessions = scope.ServiceProvider.GetRequiredService<IMapAdmissionSessions>();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var storage = scope.ServiceProvider.GetRequiredService<IMapStorage>();
		var expired = await sessions.BeginAsync(CancellationToken.None);
		var live = await sessions.BeginAsync(CancellationToken.None);
		var mapId = Guid.CreateVersion7();
		var expiredStyle = Guid.CreateVersion7();
		var liveStyle = Guid.CreateVersion7();
		var expiredFile = Guid.CreateVersion7();
		var liveFile = Guid.CreateVersion7();

		await storage.WriteAsync(expired, expiredFile, new MemoryStream([1, 2, 3]), 3,
			token => sessions.RenewAsync(expired, token), CancellationToken.None);
		await storage.WriteAsync(live, liveFile, new MemoryStream([4, 5, 6]), 3,
			token => sessions.RenewAsync(live, token), CancellationToken.None);
		await sessions.RegisterAsync(expired, mapId, expiredStyle, CancellationToken.None);
		await sessions.RegisterAsync(live, mapId, liveStyle, CancellationToken.None);
		storage.Promote(expired, expiredFile, mapId, expiredStyle);
		storage.Promote(live, liveFile, mapId, liveStyle);
		await context.Set<MapAdmissionSession>().Where(session => session.Id == expired)
			.ExecuteUpdateAsync(setters => setters.SetProperty(session => session.ExpiresAt,
				DateTimeOffset.UtcNow.AddSeconds(-1)));
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		await processor.RecoverAsync(CancellationToken.None);
		await processor.CleanupAsync(CancellationToken.None);
		Assert.False(File.Exists(storage.OriginalPath(mapId, expiredStyle)));
		Assert.True(File.Exists(storage.OriginalPath(mapId, liveStyle)));
		Assert.True(await context.Set<MapAdmissionSession>().AnyAsync(session => session.Id == live));
		Assert.False(await context.Set<MapAdmissionSession>().AnyAsync(session => session.Id == expired));
		await using var expiredScope = factory.Services.CreateAsyncScope();
		var reopened = expiredScope.ServiceProvider.GetRequiredService<IMapAdmissionSessions>();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			reopened.RenewAsync(expired, CancellationToken.None));
		await sessions.AbandonAsync(live);
		Assert.False(File.Exists(storage.OriginalPath(mapId, liveStyle)));
	}

	[Fact]
	public async Task SessionCompletionIsAtomicWithAdmissionAndCommittedReferencesProtectCleanup()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		var style = map.Styles.Single();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var sessions = scope.ServiceProvider.GetRequiredService<IMapAdmissionSessions>();
		var storage = scope.ServiceProvider.GetRequiredService<IMapStorage>();
		var sessionId = await sessions.BeginAsync(CancellationToken.None);
		var fileId = Guid.CreateVersion7();

		await sessions.RegisterAsync(sessionId, map.Id, style.Id, CancellationToken.None);
		await storage.WriteAsync(sessionId, fileId, new MemoryStream([1, 2]), 2,
			token => sessions.RenewAsync(sessionId, token), CancellationToken.None);
		storage.Promote(sessionId, fileId, map.Id, style.Id);

		await using (var transaction = await context.Database.BeginTransactionAsync())
		{
			await sessions.CompleteAsync(sessionId, CancellationToken.None);
			// Simulated failure before commit restores the lease.
		}

		Assert.True(await context.Set<MapAdmissionSession>().AnyAsync(session => session.Id == sessionId));
		await using (var transaction = await context.Database.BeginTransactionAsync())
		{
			await sessions.CompleteAsync(sessionId, CancellationToken.None);
			await transaction.CommitAsync();
		}

		await sessions.AbandonAsync(sessionId);
		await factory.Services.GetRequiredService<MapProcessor>().CleanupAsync(CancellationToken.None);
		Assert.False(await context.Set<MapAdmissionSession>().AnyAsync(session => session.Id == sessionId));
		Assert.True(File.Exists(storage.OriginalPath(map.Id, style.Id)));
	}
}
