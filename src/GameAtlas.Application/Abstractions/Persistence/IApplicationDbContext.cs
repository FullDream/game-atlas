using GameAtlas.Domain.Categories;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Markers;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
	DbSet<Game> Games { get; }
	DbSet<CategoryGroup> CategoryGroups { get; }
	DbSet<Category> Categories { get; }
	DbSet<Marker> Markers { get; }

	Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
