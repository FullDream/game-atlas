namespace GameAtlas.Application.Maps.Storage;

public interface IMapAdmissionSessions
{
	Task<Guid> BeginAsync(CancellationToken cancellationToken);
	Task RenewAsync(Guid sessionId, CancellationToken cancellationToken);
	Task RegisterAsync(Guid sessionId, Guid mapId, Guid styleId, CancellationToken cancellationToken);
	Task CompleteAsync(Guid sessionId, CancellationToken cancellationToken);
	Task AbandonAsync(Guid sessionId);
}
