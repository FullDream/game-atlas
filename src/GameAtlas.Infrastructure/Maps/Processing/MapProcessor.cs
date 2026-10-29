using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Processing;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameAtlas.Infrastructure.Maps.Processing;

public sealed class MapProcessor(IServiceScopeFactory scopes, IMapStorage storage, IMapImageProcessor images,
	ILogger<MapProcessor> logger, IMediaStorage mediaStorage, MapProgressStore progressStore,
	MapWorkQueue queue, IMapMutationLock mutationLock, IMapNotifications notifications)
{
	private readonly SemaphoreSlim writer = new(1, 1);

	public Task WaitAsync(CancellationToken cancellationToken) => queue.WaitAsync(cancellationToken);

	public async Task RecoverAsync(CancellationToken cancellationToken)
	{
		images.CheckCapabilities();
		using var lease = await mutationLock.EnterAsync(cancellationToken);
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var styles = await context.MapStyles.AsTracking()
			.Where(style => style.Status == MapProcessingStatus.Processing || style.Status == MapProcessingStatus.Queued)
			.OrderBy(style => style.CreatedAt).ThenBy(style => style.CreationSequence).ThenBy(style => style.Id)
			.ToListAsync(cancellationToken);

		foreach (var style in styles.Where(style => style.Recover()))
			context.Set<MapCleanupIntent>().Add(new MapCleanupIntent
			{
				MapId = style.MapId,
				StyleId = style.Id,
				OutputsOnly = true,
				RetryAt = DateTimeOffset.UtcNow
			});

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
		var intents = await context.Set<MapCleanupIntent>().ToArrayAsync(cancellationToken);

		foreach (var intent in intents)
			queue.ScheduleCleanup(intent.Id, intent.MapId!.Value, intent.StyleId, intent.RetryAt);

		foreach (var style in styles.Where(style => style.Status == MapProcessingStatus.Queued))
			queue.Enqueue(style.MapId, style.Id, style.AttemptId);
	}

	public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
	{
		await writer.WaitAsync(cancellationToken);

		try
		{
			await CleanupCoreAsync(cancellationToken);

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

		var progress = progressStore.Start(claim.StyleId, claim.AttemptId);
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
			generation = images.GenerateAsync(mediaStorage.AssetPath(claim.SourceAssetId),
				work, geometry, progress, attemptCancellation.Token);

			while (!generation.IsCompleted)
			{
				await Task.WhenAny(generation, Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None));

				if (cancellationToken.IsCancellationRequested)
					await generation;

				var currentProgress = progress.Value;

				if (!generation.IsCompleted && currentProgress > reportedProgress)
				{
					await notifications.ProcessingUpdatedAsync(new MapProcessingDto(claim.MapId, claim.StyleId,
						claim.AttemptId, MapProcessingStatus.Processing, currentProgress, null));
					reportedProgress = currentProgress;
				}
			}

			await generation;
			attemptCancellation.Token.ThrowIfCancellationRequested();
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
			await FailAsync(claim, cancellationToken);
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
		var due = queue.DueCleanup();

		if (due.Length == 0)
			return;

		var intents = await context.Set<MapCleanupIntent>().AsTracking().Where(intent => due.Contains(intent.Id))
			.ToListAsync(cancellationToken);

		foreach (var intent in intents)
		{
			try
			{
				if (intent.OutputsOnly && intent.MapId is { } outputMap && intent.StyleId is { } outputStyle)
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

		foreach (var id in due)
		{
			var retry = intents.FirstOrDefault(intent => intent.Id == id && context.Entry(intent).State != EntityState.Detached);

			if (retry is null)
				queue.CompleteCleanup(id);
			else
				queue.ScheduleCleanup(retry.Id, retry.MapId!.Value, retry.StyleId, retry.RetryAt);
		}
	}

	private static async Task<bool> IsReferencedAsync(ApplicationDbContext context, MapCleanupIntent intent,
		CancellationToken cancellationToken)
	{
		if (intent.StyleId is { } styleId)
		{
			return await context.MapStyles.AnyAsync(style => style.Id == styleId, cancellationToken);
		}

		return await context.Maps.AnyAsync(map => map.Id == intent.MapId, cancellationToken);
	}

	private async Task<Claim?> ClaimAsync(CancellationToken cancellationToken)
	{
		while (queue.TryTake(out var item))
		{
			using var lease = await mutationLock.EnterAsync(cancellationToken);
			await using var scope = scopes.CreateAsyncScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
			var target = await context.MapStyles.AsTracking().Include(style => style.Source)
				.Where(style => style.Id == item!.StyleId && style.AttemptId == item.AttemptId &&
					style.Status == MapProcessingStatus.Queued)
				.FirstOrDefaultAsync(cancellationToken);

			if (target is null)
				continue;

			var result = target.Claim(target.AttemptId);

			if (result.IsFailure)
				continue;

			await context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			var dto = target.ToDto(progressStore);

			lease.Dispose();
			await notifications.StyleUpdatedAsync(target.MapId, dto);

			return new Claim(target.MapId, target.Id, target.AttemptId, target.SourceAssetId, target.Width, target.Height);
		}

		return null;
	}

	private async Task PublishAsync(Claim claim, CancellationToken cancellationToken)
	{
		using var lease = await mutationLock.EnterAsync(cancellationToken);
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var style = await LoadAsync(context, claim, cancellationToken);

		MapCleanupIntent? cleanup = null;

		if (style is null || style.Publish(claim.AttemptId).IsFailure)
		{
			cleanup = new MapCleanupIntent
			{
				MapId = claim.MapId,
				StyleId = claim.StyleId,
				RetryAt = DateTimeOffset.UtcNow
			};
			context.Set<MapCleanupIntent>().Add(cleanup);
		}

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		if (cleanup is not null)
		{
			queue.ScheduleCleanup(cleanup.Id, claim.MapId, claim.StyleId, cleanup.RetryAt);

			return;
		}

		var dto = style!.ToDto(progressStore);

		lease.Dispose();
		await notifications.StyleUpdatedAsync(claim.MapId, dto);
	}

	private async Task FailAsync(Claim claim, CancellationToken cancellationToken)
	{
		using var lease = await mutationLock.EnterAsync(cancellationToken);
		await using var scope = scopes.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		var style = await LoadAsync(context, claim, cancellationToken);

		if (style is null)
			return;

		var result = style.Fail(claim.AttemptId);

		if (result.IsFailure)
			return;

		var cleanup = new MapCleanupIntent
		{
			MapId = claim.MapId,
			StyleId = claim.StyleId,
			OutputsOnly = true,
			RetryAt = DateTimeOffset.UtcNow
		};
		context.Set<MapCleanupIntent>().Add(cleanup);

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);
		queue.ScheduleCleanup(cleanup.Id, claim.MapId, claim.StyleId, cleanup.RetryAt);
		var dto = style.ToDto(progressStore);

		lease.Dispose();
		await notifications.StyleUpdatedAsync(claim.MapId, dto);
	}

	private static Task<MapStyle?> LoadAsync(ApplicationDbContext context, Claim claim,
		CancellationToken cancellationToken) => context.MapStyles.AsTracking().Include(style => style.Source)
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

	private sealed record Claim(Guid MapId, Guid StyleId, Guid AttemptId, Guid SourceAssetId, int Width, int Height);
}
