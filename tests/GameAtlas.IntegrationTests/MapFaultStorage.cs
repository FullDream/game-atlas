using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Result;

namespace GameAtlas.IntegrationTests;

internal sealed class MapFaultStorage(IMapStorage inner, MapStorageFaults faults) : IMapStorage
{
	public string PublicTilesRoot => inner.PublicTilesRoot;

	public string WorkPath(Guid map, Guid style, Guid attempt) => inner.WorkPath(map, style, attempt);
	public string TilesPath(Guid map, Guid style) => inner.TilesPath(map, style);

	public void Publish(Guid map, Guid style, Guid attempt)
	{
		inner.Publish(map, style, attempt);

		if (faults.FailAfterPublicationMove)
			throw new IOException("Injected failure after immutable directory move.");
	}

	public void DeleteWork(Guid map, Guid style) => inner.DeleteWork(map, style);
	public void DeleteTiles(Guid map, Guid style) => inner.DeleteTiles(map, style);

	public void DeleteOwned(Guid map, Guid? style = null)
	{
		if (faults.FailCleanup)
			throw new IOException("Injected temporary cleanup failure.");

		inner.DeleteOwned(map, style);
	}
}
