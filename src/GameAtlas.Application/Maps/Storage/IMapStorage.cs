namespace GameAtlas.Application.Maps.Storage;

public interface IMapStorage
{
	string PublicTilesRoot { get; }

	string WorkPath(Guid mapId, Guid styleId, Guid attemptId);
	string TilesPath(Guid mapId, Guid styleId);

	void Publish(Guid mapId, Guid styleId, Guid attemptId);
	void DeleteOwned(Guid mapId, Guid? styleId = null);
	void DeleteWork(Guid mapId, Guid styleId);
	void DeleteTiles(Guid mapId, Guid styleId);
}
