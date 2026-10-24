using GameAtlas.Application.Maps.Storage;
using GameAtlas.Infrastructure.Maps.Storage;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Maps.Persistence;

internal sealed class MapAdmissionSessions(ApplicationDbContext requestContext, IServiceScopeFactory scopes,
	IOptions<MapStorageOptions> options, IMapStorage storage, ILogger<MapAdmissionSessions> logger)
	: IMapAdmissionSessions
{
	private readonly Dictionary<Guid, DateTimeOffset> refreshed = [];

	public async Task<Guid> BeginAsync(CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var session = new MapAdmissionSession
		{
			ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(options.Value.LeaseSeconds)
		};

		context.Set<MapAdmissionSession>().Add(session);
		await context.SaveChangesAsync(cancellationToken);
		refreshed[session.Id] = DateTimeOffset.UtcNow;

		return session.Id;
	}

	public async Task RenewAsync(Guid sessionId, CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.UtcNow;

		if (refreshed.TryGetValue(sessionId, out var lastRefresh) && now - lastRefresh < TimeSpan.FromSeconds(10))
			return;

		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var updated = await context.Set<MapAdmissionSession>()
			.Where(session => session.Id == sessionId && session.ExpiresAt > now)
			.ExecuteUpdateAsync(setters => setters.SetProperty(session => session.ExpiresAt,
				now.AddSeconds(options.Value.LeaseSeconds)), cancellationToken);

		if (updated != 1)
			throw new OperationCanceledException("The upload session has expired.", cancellationToken);

		refreshed[sessionId] = now;
	}

	public async Task RegisterAsync(Guid sessionId, Guid mapId, Guid styleId,
		CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var now = DateTimeOffset.UtcNow;
		var updated = await context.Set<MapAdmissionSession>()
			.Where(session => session.Id == sessionId && session.ExpiresAt > now)
			.ExecuteUpdateAsync(setters => setters.SetProperty(session => session.ExpiresAt,
				now.AddSeconds(options.Value.LeaseSeconds)), cancellationToken);

		if (updated != 1)
			throw new OperationCanceledException("The upload session has expired.", cancellationToken);

		context.Set<MapAdmissionStyle>().Add(new MapAdmissionStyle
		{
			SessionId = sessionId,
			MapId = mapId,
			StyleId = styleId
		});

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
	}

	public async Task CompleteAsync(Guid sessionId, CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.UtcNow;
		var deleted = await requestContext.Set<MapAdmissionSession>()
			.Where(session => session.Id == sessionId && session.ExpiresAt > now)
			.ExecuteDeleteAsync(cancellationToken);

		if (deleted != 1)
			throw new OperationCanceledException("The upload session has expired.", cancellationToken);

		refreshed.Remove(sessionId);
	}

	public async Task AbandonAsync(Guid sessionId)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync();
		var sources = await context.Set<MapAdmissionStyle>().Where(source => source.SessionId == sessionId)
			.ToListAsync();

		foreach (var source in sources)
		{
			context.Set<MapCleanupIntent>().Add(new MapCleanupIntent
			{
				MapId = source.MapId,
				StyleId = source.StyleId,
				RetryAt = DateTimeOffset.UtcNow
			});
		}

		context.Set<MapCleanupIntent>().Add(new MapCleanupIntent
		{
			SessionId = sessionId,
			RetryAt = DateTimeOffset.UtcNow
		});

		await context.Set<MapAdmissionSession>().Where(session => session.Id == sessionId).ExecuteDeleteAsync();
		await context.SaveChangesAsync();
		await transaction.CommitAsync();
		refreshed.Remove(sessionId);

		try
		{
			foreach (var source in sources)
			{
				var referenced = await context.MapStyles.AnyAsync(style => style.Id == source.StyleId);

				if (!referenced)
					storage.DeleteOwned(source.MapId, source.StyleId);
			}

			storage.DeleteStaging(sessionId);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			logger.LogWarning(exception, "Map admission session {SessionId} cleanup is deferred to durable retry",
				sessionId);
		}
	}
}
