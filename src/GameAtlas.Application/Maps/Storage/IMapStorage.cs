namespace GameAtlas.Application.Maps.Storage;

public interface IMapStorage
{
	string PublicTilesRoot { get; }

	string StagingPath(Guid sessionId, Guid fileId);
	string OriginalPath(Guid mapId, Guid styleId);
	string WorkPath(Guid mapId, Guid styleId, Guid attemptId);
	string TilesPath(Guid mapId, Guid styleId);

	Task<Domain.Result.Result<long>> WriteAsync(Guid sessionId, Guid fileId, Stream source, long maximumBytes,
		Func<CancellationToken, Task> renewLease, CancellationToken cancellationToken);

	void Promote(Guid sessionId, Guid fileId, Guid mapId, Guid styleId);
	void Publish(Guid mapId, Guid styleId, Guid attemptId);
	void DeleteStaging(Guid sessionId);
	void DeleteOwned(Guid mapId, Guid? styleId = null);
	void DeleteWork(Guid mapId, Guid styleId);
	void DeleteTiles(Guid mapId, Guid styleId);
}
