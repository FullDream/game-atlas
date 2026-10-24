using GameAtlas.Application.Maps.Storage;
using GameAtlas.Infrastructure.Persistence;

namespace GameAtlas.Infrastructure.Maps.Persistence;

internal sealed class MapCleanup(ApplicationDbContext context) : IMapCleanup
{
	public void Schedule(Guid mapId, Guid? styleId = null) =>
		context.Set<MapCleanupIntent>().Add(new MapCleanupIntent
		{
			MapId = mapId,
			StyleId = styleId,
			RetryAt = DateTimeOffset.UtcNow
		});
}
