using GameAtlas.Application.Maps.Processing;

namespace GameAtlas.Infrastructure.Maps.Persistence;

public sealed class MapMutationLock : IMapMutationLock
{
	// SQLite has one writer. Callers release this lock before awaiting notification delivery.
	private readonly SemaphoreSlim semaphore = new(1, 1);

	public async ValueTask<IDisposable> EnterAsync(CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken);

		return new Lease(semaphore);
	}

	private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
	{
		private int disposed;

		public void Dispose()
		{
			if (Interlocked.Exchange(ref disposed, 1) == 0)
				semaphore.Release();
		}
	}
}
