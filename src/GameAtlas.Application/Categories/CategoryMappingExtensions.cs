using GameAtlas.Domain.Categories;

namespace GameAtlas.Application.Categories;

internal static class CategoryMappingExtensions
{
	internal static IQueryable<CategoryDto> ToDto(this IQueryable<Category> categories) =>
		categories.Select(category => new CategoryDto(category.Id, category.CategoryGroupId, category.Name,
			category.Icon, category.Color, category.Order, 0, category.CreatedAt, category.UpdatedAt));

	internal static CategoryDto ToDto(this Category category) => new(
		category.Id, category.CategoryGroupId, category.Name, category.Icon, category.Color, category.Order,
		0, category.CreatedAt, category.UpdatedAt);
}
