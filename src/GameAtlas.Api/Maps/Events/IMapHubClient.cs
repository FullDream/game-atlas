using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Processing;

namespace GameAtlas.Api.Maps.Events;

public interface IMapHubClient
{
	Task StyleUpdated(MapStyleDto style);
	Task ProcessingUpdated(MapProcessingDto processing);
}
