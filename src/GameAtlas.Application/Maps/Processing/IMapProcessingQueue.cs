namespace GameAtlas.Application.Maps.Processing;

public interface IMapProcessingQueue
{
	void Enqueue(Guid mapId, Guid styleId, Guid attemptId);
}
