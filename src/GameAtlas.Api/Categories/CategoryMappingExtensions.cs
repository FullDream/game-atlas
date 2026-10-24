using GameAtlas.Application.Categories.CreateCategory;
using GameAtlas.Application.Categories.UpdateCategory;

namespace GameAtlas.Api.Categories;

internal static class CategoryMappingExtensions
{
	internal static CreateCategory ToCommand(this CreateCategoryRequest request, Guid gameId, Guid mapId,
		Guid groupId) =>
		new(gameId, mapId, groupId, request.Name, request.IconAssetId, request.Color, request.Order);

	internal static UpdateCategory ToCommand(this UpdateCategoryRequest request, Guid gameId, Guid mapId,
		Guid groupId, Guid id) =>
		new(gameId, mapId, groupId, id, request.Name, request.IconAssetId, request.Color, request.Order,
			request.CategoryGroupId);
}
