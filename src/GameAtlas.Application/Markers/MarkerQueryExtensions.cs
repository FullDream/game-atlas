using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers;

internal static class MarkerQueryExtensions
{
	internal static IQueryable<Marker> ForMap(this IQueryable<Marker> markers, IApplicationDbContext context,
		Guid gameId, Guid mapId)
	{
		var categories = context.Categories.ForMap(context, gameId, mapId);

		return markers.Where(marker => categories.Any(category => category.Id == marker.CategoryId));
	}
}
