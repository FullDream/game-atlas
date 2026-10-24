using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Result;

namespace GameAtlas.IntegrationTests;

internal sealed class MapFaultStorage(IMapStorage inner, MapStorageFaults faults) : IMapStorage
{
	public string PublicTilesRoot => inner.PublicTilesRoot;

	public string StagingPath(Guid session, Guid file) => inner.StagingPath(session, file);
	public string OriginalPath(Guid map, Guid style) => inner.OriginalPath(map, style);
	public string WorkPath(Guid map, Guid style, Guid attempt) => inner.WorkPath(map, style, attempt);
	public string TilesPath(Guid map, Guid style) => inner.TilesPath(map, style);

	public Task<Result<long>> WriteAsync(Guid session, Guid file, Stream source, long maximum,
		Func<CancellationToken, Task> renew, CancellationToken cancellationToken) =>
		inner.WriteAsync(session, file, source, maximum, renew, cancellationToken);

	public void Promote(Guid session, Guid file, Guid map, Guid style)
	{
		faults.PromotionCount++;

		if (faults.PromotionCount == faults.FailPromotionNumber && !faults.FailAfterPromotion)
			throw new IOException("Injected promotion failure.");

		inner.Promote(session, file, map, style);

		if (faults.PromotionCount == faults.FailPromotionNumber && faults.FailAfterPromotion)
			throw new IOException("Injected failure after original promotion.");
	}

	public void Publish(Guid map, Guid style, Guid attempt)
	{
		inner.Publish(map, style, attempt);

		if (faults.FailAfterPublicationMove)
			throw new IOException("Injected failure after immutable directory move.");
	}

	public void DeleteStaging(Guid session) => inner.DeleteStaging(session);
	public void DeleteWork(Guid map, Guid style) => inner.DeleteWork(map, style);
	public void DeleteTiles(Guid map, Guid style) => inner.DeleteTiles(map, style);

	public void DeleteOwned(Guid map, Guid? style = null)
	{
		if (faults.FailCleanup)
			throw new IOException("Injected temporary cleanup failure.");

		inner.DeleteOwned(map, style);
	}
}
