using GameAtlas.Application.Categories.CreateCategory;
using GameAtlas.Application.Categories.UpdateCategory;

namespace GameAtlas.Api.Categories;

internal static class CategoryMappingExtensions
{
	internal static CreateCategory ToCommand(this CreateCategoryRequest request, Guid gameId, Guid groupId) =>
		new(gameId, groupId, request.Name, request.Icon, request.Color, request.Order);

	internal static UpdateCategory ToCommand(this UpdateCategoryRequest request, Guid gameId, Guid groupId, Guid id) =>
		new(gameId, groupId, id, request.Name, request.Icon, request.Color, request.Order, request.CategoryGroupId);
}
