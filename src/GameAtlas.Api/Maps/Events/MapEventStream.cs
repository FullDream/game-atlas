using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using GameAtlas.Api.Results;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.GetMap;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Api.Maps.Events;

internal sealed class MapEventStream(IServiceScopeFactory scopes, ILogger<MapEventStream> logger)
{
	public async Task<IResult> OpenAsync(Guid gameId, Guid mapId, HttpContext context,
		CancellationToken cancellationToken)
	{
		var result = await ReadAsync(gameId, mapId, cancellationToken);

		if (result.IsFailure)
			return new OutcomeResult(result.Errors);

		context.Response.Headers["X-Accel-Buffering"] = "no";

		return TypedResults.ServerSentEvents(EnumerateAsync(gameId, mapId, result.Value, cancellationToken));
	}

	private async IAsyncEnumerable<SseItem<object>> EnumerateAsync(Guid gameId, Guid mapId, MapDto initial,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var snapshot = ToSnapshot(initial);
		var previous = JsonSerializer.Serialize(snapshot);
		var heartbeatAt = DateTimeOffset.UtcNow.AddSeconds(15);

		yield return new SseItem<object>(snapshot, "snapshot");

		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

		while (await timer.WaitForNextTickAsync(cancellationToken))
		{
			MapDto? current = null;
			var failed = false;

			try
			{
				var result = await ReadAsync(gameId, mapId, cancellationToken);

				if (result.IsSuccess)
					current = result.Value;
				else if (!result.Errors.All(error => error.Type == Domain.Result.ErrorType.NotFound))
					failed = true;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				yield break;
			}
			catch (Exception exception)
			{
				logger.LogError(exception, "Map {MapId} event stream failed", initial.Id);
				failed = true;
			}

			if (failed)
			{
				yield return new SseItem<object>(new
				{
					MapId = initial.Id,
					Code = "Map.Stream.Failed",
					Message = "The event stream could not continue. Reconnect to obtain current state."
				}, "error");
				yield break;
			}

			if (current is null || current.Id != initial.Id)
			{
				yield return new SseItem<object>(new { MapId = initial.Id }, "mapDeleted");
				yield break;
			}

			snapshot = ToSnapshot(current);
			var serialized = JsonSerializer.Serialize(snapshot);

			if (serialized != previous)
			{
				previous = serialized;

				yield return new SseItem<object>(snapshot, "progress");
			}

			if (DateTimeOffset.UtcNow >= heartbeatAt)
			{
				heartbeatAt = DateTimeOffset.UtcNow.AddSeconds(15);

				yield return new SseItem<object>(new { MapId = initial.Id }, "heartbeat");
			}
		}
	}

	private static MapStreamSnapshot ToSnapshot(MapDto map) => new(map.Id, map.DefaultStyleId, map.Styles);

	private async Task<Result<MapDto>> ReadAsync(Guid gameId, Guid mapId, CancellationToken cancellationToken)
	{
		await using var scope = scopes.CreateAsyncScope();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

		return await mediator.Send(new GetMap(gameId, mapId), cancellationToken);
	}
}
