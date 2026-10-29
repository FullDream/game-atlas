using GameAtlas.Application.Maps.Storage;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;

namespace GameAtlas.Infrastructure.Maps.Persistence;

internal sealed class MapCleanup(ApplicationDbContext context, MapWorkQueue queue)
	: IMapCleanup
{
	private readonly List<MapCleanupIntent> pending = [];

	public void Schedule(Guid mapId, Guid? styleId = null)
	{
		var intent = new MapCleanupIntent { MapId = mapId, StyleId = styleId, RetryAt = DateTimeOffset.UtcNow };

		context.Set<MapCleanupIntent>().Add(intent);
		pending.Add(intent);
	}

	public void Dispatch()
	{
		foreach (var intent in pending)
		{
			queue.ScheduleCleanup(intent.Id, intent.MapId!.Value, intent.StyleId, intent.RetryAt);
		}

		pending.Clear();
	}
}
