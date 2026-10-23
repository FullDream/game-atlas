namespace GameAtlas.Application.Abstractions.Persistence;

public interface IApplicationTransaction : IAsyncDisposable
{
	Task CommitAsync(CancellationToken cancellationToken = default);
}
