using GameAtlas.Domain.Games;
using GameAtlas.Domain.CategoryGroups;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
	DbSet<Game> Games { get; }
	DbSet<CategoryGroup> CategoryGroups { get; }

	Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
