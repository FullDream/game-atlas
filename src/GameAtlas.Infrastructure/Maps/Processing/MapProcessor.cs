using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameAtlas.Infrastructure.Maps.Processing;

public sealed class MapProcessor(IServiceScopeFactory scopes, IMapStorage storage, IMapImageProcessor images,
	ILogger<MapProcessor> logger)
{
	private readonly SemaphoreSlim writer = new(1, 1);

	public async Task RecoverAsync(CancellationToken cancellationToken)
	{
		images.CheckCapabilities();
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var styles = await context.MapStyles.AsTracking()
			.Where(style => style.Status == MapProcessingStatus.Processing)
			.ToListAsync(cancellationToken);

		foreach (var style in styles)
			style.Recover();

		var now = DateTimeOffset.UtcNow;
		var expired = await context.Set<MapAdmissionSession>().Include(session => session.Sources)
			.Where(session => session.ExpiresAt <= now).ToListAsync(cancellationToken);

		foreach (var session in expired)
		{
			foreach (var source in session.Sources)
			{
				context.Set<MapCleanupIntent>().Add(new MapCleanupIntent
				{
					MapId = source.MapId,
					StyleId = source.StyleId,
					RetryAt = now
				});
			}

			context.Set<MapCleanupIntent>().Add(new MapCleanupIntent { SessionId = session.Id, RetryAt = now });
			context.Remove(session);
		}

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
	}

	public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
	{
		await writer.WaitAsync(cancellationToken);

		try
		{
			return await ProcessNextAsync(cancellationToken);
		}
		finally
		{
			writer.Release();
		}
	}

	private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
	{
		var claim = await ClaimAsync(cancellationToken);

		if (claim is null)
			return false;

		var progress = new MapProgressBuffer();
		using var attemptCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		Task? generation = null;
		var reportedProgress = 0;

		try
		{
			storage.DeleteWork(claim.MapId, claim.StyleId);
			storage.DeleteTiles(claim.MapId, claim.StyleId);
			var geometryResult = MapGeometry.Create(claim.Width, claim.Height);

			if (geometryResult.IsFailure)
				throw new InvalidDataException("The persisted map dimensions are invalid.");

			var geometry = geometryResult.Value;
			var work = storage.WorkPath(claim.MapId, claim.StyleId, claim.AttemptId);
			generation = images.GenerateAsync(storage.OriginalPath(claim.MapId, claim.StyleId),
				work, geometry, progress, attemptCancellation.Token);

			while (!generation.IsCompleted)
			{
				await Task.WhenAny(generation, Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None));

				if (cancellationToken.IsCancellationRequested)
					await generation;

				var currentProgress = progress.Value;

				if (!generation.IsCompleted && currentProgress > reportedProgress)
				{
					await SaveProgressAsync(claim, currentProgress, cancellationToken);
					reportedProgress = currentProgress;
				}
			}

			await generation;
			ValidateOutput(work, geometry, cancellationToken);
			storage.Publish(claim.MapId, claim.StyleId, claim.AttemptId);
			await PublishAsync(claim, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			await StopGenerationAsync(generation, attemptCancellation);

			throw;
		}
		catch (Exception exception)
		{
			await StopGenerationAsync(generation, attemptCancellation);
			logger.LogError(exception, "Map style {StyleId} processing failed", claim.StyleId);
			await FailAsync(claim, progress.Value, cancellationToken);
		}

		return true;
	}

	private static async Task StopGenerationAsync(Task? generation, CancellationTokenSource cancellation)
	{
		await cancellation.CancelAsync();

		if (generation is null)
			return;

		try
		{
			await generation;
		}
		catch (Exception)
		{
			// Observe the native task before releasing the writer; the original failure is handled by the caller.
		}
	}

	public async Task CleanupAsync(CancellationToken cancellationToken)
	{
		await writer.WaitAsync(cancellationToken);

		try
		{
			await CleanupCoreAsync(cancellationToken);
		}
		finally
		{
			writer.Release();
		}
	}

	private async Task CleanupCoreAsync(CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var now = DateTimeOffset.UtcNow;
		var intents = await context.Set<MapCleanupIntent>().AsTracking().Where(intent => intent.RetryAt <= now)
			.OrderBy(intent => intent.RetryAt).Take(100).ToListAsync(cancellationToken);

		foreach (var intent in intents)
		{
			try
			{
				if (intent.SessionId is { } sessionId)
				{
					var live = await context.Set<MapAdmissionSession>().AnyAsync(session => session.Id == sessionId,
						cancellationToken);

					if (!live)
						storage.DeleteStaging(sessionId);
				}
				else if (intent.OutputsOnly && intent.MapId is { } outputMap && intent.StyleId is { } outputStyle)
				{
					var published = await context.MapStyles.AnyAsync(style =>
						style.Id == outputStyle && style.Status == MapProcessingStatus.Ready, cancellationToken);

					if (!published)
					{
						storage.DeleteTiles(outputMap, outputStyle);
						storage.DeleteWork(outputMap, outputStyle);
					}
				}
				else if (intent.MapId is { } mapId && !await IsReferencedAsync(context, intent, cancellationToken))
				{
					storage.DeleteOwned(mapId, intent.StyleId);
				}

				context.Remove(intent);
			}
			catch (IOException exception)
			{
				logger.LogWarning(exception, "Map cleanup intent {IntentId} will be retried", intent.Id);
				intent.Attempts++;
				intent.RetryAt = now.AddSeconds(Math.Min(60, intent.Attempts * 5));
			}
			catch (UnauthorizedAccessException exception)
			{
				logger.LogWarning(exception, "Map cleanup intent {IntentId} cannot access storage", intent.Id);
				intent.Attempts++;
				intent.RetryAt = now.AddSeconds(60);
			}
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	private static async Task<bool> IsReferencedAsync(ApplicationDbContext context, MapCleanupIntent intent,
		CancellationToken cancellationToken)
	{
		if (intent.StyleId is { } styleId)
		{
			var referenced = await context.MapStyles.AnyAsync(style =>
				style.Id == styleId, cancellationToken);

			var liveUpload = await context.Set<MapAdmissionStyle>().AnyAsync(source => source.StyleId == styleId,
				cancellationToken);

			return referenced || liveUpload;
		}

		return await context.Maps.AnyAsync(map => map.Id == intent.MapId, cancellationToken);
	}

	private async Task<Claim?> ClaimAsync(CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var target = await context.MapStyles.AsTracking()
			.Where(style => style.Status == MapProcessingStatus.Queued)
			.OrderBy(style => style.CreatedAt).ThenBy(style => style.CreationSequence).ThenBy(style => style.Id)
			.FirstOrDefaultAsync(cancellationToken);

		if (target is null)
			return null;

		var result = target.Claim(target.AttemptId);

		if (result.IsFailure)
			return null;

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return new Claim(target.MapId, target.Id, target.AttemptId, target.Width, target.Height);
	}

	private async Task SaveProgressAsync(Claim claim, int progress, CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var style = await LoadAsync(context, claim, cancellationToken);

		if (style is null || style.ReportProgress(claim.AttemptId, progress).IsFailure)
			return;

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
	}

	private async Task PublishAsync(Claim claim, CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var style = await LoadAsync(context, claim, cancellationToken);

		if (style is null || style.Publish(claim.AttemptId).IsFailure)
		{
			context.Set<MapCleanupIntent>().Add(new MapCleanupIntent
			{
				MapId = claim.MapId,
				StyleId = claim.StyleId,
				RetryAt = DateTimeOffset.UtcNow
			});
		}

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
	}

	private async Task FailAsync(Claim claim, int progress, CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var style = await LoadAsync(context, claim, cancellationToken);

		if (style is null)
			return;

		style.ReportProgress(claim.AttemptId, progress);
		var result = style.Fail(claim.AttemptId);

		if (result.IsFailure)
			return;

		context.Set<MapCleanupIntent>().Add(new MapCleanupIntent
		{
			MapId = claim.MapId,
			StyleId = claim.StyleId,
			OutputsOnly = true,
			RetryAt = DateTimeOffset.UtcNow
		});

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
	}

	private static Task<MapStyle?> LoadAsync(ApplicationDbContext context, Claim claim,
		CancellationToken cancellationToken) => context.MapStyles.AsTracking()
		.Where(style => style.Id == claim.StyleId && style.MapId == claim.MapId)
		.SingleOrDefaultAsync(cancellationToken);

	private static void ValidateOutput(string work, MapGeometry geometry, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "manifest.json")));

		using (manifest)
		{
			if (manifest.RootElement.GetProperty("Width").GetInt32() != geometry.Width ||
				manifest.RootElement.GetProperty("Height").GetInt32() != geometry.Height ||
				manifest.RootElement.GetProperty("MaxZoom").GetInt32() != geometry.MaxZoom ||
				manifest.RootElement.GetProperty("TileSize").GetInt32() != MapGeometry.TileSize)
				throw new InvalidDataException("The generated manifest does not match its source.");
		}

		for (var zoom = 0; zoom <= geometry.MaxZoom; zoom++)
		{
			for (var x = 0; x < geometry.Columns(zoom); x++)
			{
				for (var y = 0; y < geometry.Rows(zoom); y++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var bytes = File.ReadAllBytes(Path.Combine(work, $"{zoom}", $"{x}", $"{y}.webp"));
					using var tile = NetVips.Image.NewFromBuffer(bytes);

					if (tile.Width != MapGeometry.TileSize || tile.Height != MapGeometry.TileSize)
						throw new InvalidDataException("The generated tile dimensions are invalid.");

					// Force decoding before making the style's generated tiles visible.
					_ = tile.Avg();
				}
			}
		}
	}

	private sealed record Claim(Guid MapId, Guid StyleId, Guid AttemptId, int Width, int Height);
}
