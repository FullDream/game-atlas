using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Categories;

internal static class CategoryMappingExtensions
{
	internal static IQueryable<CategoryDto> ToDto(this IQueryable<Category> categories, IQueryable<Marker> markers) =>
		categories.Select(category => new CategoryDto(
			category.Id,
			category.CategoryGroupId,
			category.Name,
			category.Icon,
			category.Color,
			category.Order,
			markers.Count(marker => marker.CategoryId == category.Id),
			category.CreatedAt,
			category.UpdatedAt));

	internal static CategoryDto ToDto(this Category category, int markersCount) => new(
		category.Id,
		category.CategoryGroupId,
		category.Name,
		category.Icon,
		category.Color,
		category.Order,
		markersCount,
		category.CreatedAt,
		category.UpdatedAt);
}
