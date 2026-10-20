namespace GameAtlas.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
