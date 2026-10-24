namespace GameAtlas.Application.Maps.Storage;

public interface IMapCleanup
{
	void Schedule(Guid mapId, Guid? styleId = null);
}
