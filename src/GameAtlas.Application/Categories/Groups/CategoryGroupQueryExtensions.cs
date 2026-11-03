using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps;
using GameAtlas.Domain.Categories;

namespace GameAtlas.Application.Categories.Groups;

internal static class CategoryGroupQueryExtensions
{
	internal static IQueryable<CategoryGroup> ForMap(this IQueryable<CategoryGroup> groups,
		IApplicationDbContext context, Guid gameId, Guid mapId)
	{
		var maps = context.Maps.ForMap(gameId, mapId);

		return groups.Where(group => group.MapId == mapId && maps.Any(map => map.Id == group.MapId));
	}
}
