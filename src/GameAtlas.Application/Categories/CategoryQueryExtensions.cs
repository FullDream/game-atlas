using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Categories;

namespace GameAtlas.Application.Categories;

internal static class CategoryQueryExtensions
{
	internal static IQueryable<Category> ForGame(this IQueryable<Category> categories, IApplicationDbContext context,
		Guid gameId) =>
		categories.Where(category => context.CategoryGroups
			.Any(group => group.Id == category.CategoryGroupId && group.GameId == gameId));
}
