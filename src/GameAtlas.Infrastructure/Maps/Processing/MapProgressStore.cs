using GameAtlas.Application.Maps.Processing;
using GameAtlas.Domain.Maps;

namespace GameAtlas.Infrastructure.Maps.Processing;

public sealed class MapProgressStore : IMapProgress
{
	private const int Capacity = 4096;
	private readonly Lock sync = new();
	private readonly Dictionary<Guid, (Guid AttemptId, MapProgressBuffer Buffer)> entries = [];
	private readonly Queue<(Guid StyleId, Guid AttemptId)> order = [];

	internal MapProgressBuffer Start(Guid styleId, Guid attemptId)
	{
		lock (sync)
		{
			var buffer = new MapProgressBuffer();
			entries[styleId] = (attemptId, buffer);
			order.Enqueue((styleId, attemptId));

			while (order.Count > Capacity)
			{
				var expired = order.Dequeue();

				if (entries.TryGetValue(expired.StyleId, out var entry) && entry.AttemptId == expired.AttemptId)
					entries.Remove(expired.StyleId);
			}

			return buffer;
		}
	}

	public int Read(MapStyle style)
	{
		if (style.Status == MapProcessingStatus.Ready)
			return 100;

		if (style.Status == MapProcessingStatus.Queued)
			return 0;

		lock (sync)
		{
			return entries.TryGetValue(style.Id, out var entry) && entry.AttemptId == style.AttemptId
				? entry.Buffer.Value : 0;
		}
	}
}
