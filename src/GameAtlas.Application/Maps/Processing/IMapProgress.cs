using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.Processing;

public interface IMapProgress
{
	int Read(MapStyle style);
}
