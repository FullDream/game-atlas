using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Domain.Categories;

namespace GameAtlas.Application.Categories;

internal static class CategoryQueryExtensions
{
	internal static IQueryable<Category> ForMap(this IQueryable<Category> categories, IApplicationDbContext context,
		Guid gameId, Guid mapId)
	{
		var groups = context.CategoryGroups.ForMap(context, gameId, mapId);

		return categories.Where(category => groups.Any(group => group.Id == category.CategoryGroupId));
	}
}
