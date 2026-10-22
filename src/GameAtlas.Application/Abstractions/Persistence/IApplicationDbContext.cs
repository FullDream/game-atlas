using GameAtlas.Domain.Games;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
	DbSet<Game> Games { get; }

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
