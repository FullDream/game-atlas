using System.Threading.Channels;
using GameAtlas.Application.Maps.Processing;

namespace GameAtlas.Infrastructure.Maps.Processing;

public sealed class MapWorkQueue : IMapProcessingQueue
{
	private readonly Lock sync = new();
	private readonly Queue<MapWorkItem> jobs = [];
	private readonly HashSet<MapWorkItem> scheduled = [];
	private readonly Dictionary<Guid, CleanupWork> cleanup = [];
	private readonly Channel<bool> signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
	{
		FullMode = BoundedChannelFullMode.DropWrite
	});

	public void Enqueue(Guid mapId, Guid styleId, Guid attemptId) =>
		Enqueue(new MapWorkItem(mapId, styleId, attemptId));

	public void Enqueue(MapWorkItem item)
	{
		lock (sync)
		{
			if (scheduled.Add(item))
				jobs.Enqueue(item);
		}

		signal.Writer.TryWrite(true);
	}

	internal bool TryTake(out MapWorkItem? item)
	{
		lock (sync)
		{
			var count = jobs.Count;

			while (count-- > 0)
			{
				var candidate = jobs.Dequeue();

				if (cleanup.Values.Any(work => work.MapId == candidate.MapId &&
					(work.StyleId is null || work.StyleId == candidate.StyleId)))
				{
					jobs.Enqueue(candidate);
					continue;
				}

				scheduled.Remove(candidate);
				item = candidate;

				return true;
			}

			item = null;

			return false;
		}
	}

	internal void ScheduleCleanup(Guid id, Guid mapId, Guid? styleId, DateTimeOffset due)
	{
		lock (sync)
			cleanup[id] = new CleanupWork(id, mapId, styleId, due);

		signal.Writer.TryWrite(true);
	}

	internal Guid[] DueCleanup()
	{
		lock (sync)
			return cleanup.Values.Where(work => work.Due <= DateTimeOffset.UtcNow).Select(work => work.Id).ToArray();
	}

	internal void CompleteCleanup(Guid id)
	{
		lock (sync)
			cleanup.Remove(id);

		signal.Writer.TryWrite(true);
	}

	public async Task WaitAsync(CancellationToken cancellationToken)
	{
		TimeSpan? delay;

		lock (sync)
		{
			if (jobs.Any(job => !cleanup.Values.Any(work => work.MapId == job.MapId &&
				(work.StyleId is null || work.StyleId == job.StyleId))))
				return;

			delay = cleanup.Count == 0 ? null : cleanup.Values.Min(work => work.Due) - DateTimeOffset.UtcNow;
		}

		if (delay <= TimeSpan.Zero)
			return;

		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		if (delay is { } duration)
			timeout.CancelAfter(duration);

		try
		{
			await signal.Reader.ReadAsync(timeout.Token);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			// A known cleanup intent became due.
		}
	}

	private sealed record CleanupWork(Guid Id, Guid MapId, Guid? StyleId, DateTimeOffset Due);
}
