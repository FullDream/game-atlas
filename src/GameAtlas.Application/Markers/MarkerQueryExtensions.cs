using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers;

internal static class MarkerQueryExtensions
{
	internal static IQueryable<Marker> ForGame(this IQueryable<Marker> markers, IApplicationDbContext context,
		Guid gameId)
	{
		var categories = context.Categories.ForGame(context, gameId);

		return markers.Where(marker => categories.Any(category => category.Id == marker.CategoryId));
	}
}
