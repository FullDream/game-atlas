namespace GameAtlas.Application.Maps.Processing;

public interface IMapMutationLock
{
	ValueTask<IDisposable> EnterAsync(CancellationToken cancellationToken);
}
