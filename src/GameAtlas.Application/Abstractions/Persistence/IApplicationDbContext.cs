using GameAtlas.Domain.Categories;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
	DbSet<Game> Games { get; }
	DbSet<CategoryGroup> CategoryGroups { get; }
	DbSet<Category> Categories { get; }
	DbSet<Marker> Markers { get; }
	DbSet<Map> Maps { get; }
	DbSet<MapStyle> MapStyles { get; }
	DbSet<MediaFolder> MediaFolders { get; }
	DbSet<MediaAsset> MediaAssets { get; }

	Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
