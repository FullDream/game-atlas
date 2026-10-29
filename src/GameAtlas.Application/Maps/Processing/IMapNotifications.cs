namespace GameAtlas.Application.Maps.Processing;

public interface IMapNotifications
{
	Task StyleUpdatedAsync(Guid mapId, MapStyleDto style);
	Task ProcessingUpdatedAsync(MapProcessingDto processing);
}
