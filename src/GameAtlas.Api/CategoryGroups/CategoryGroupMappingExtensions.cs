using GameAtlas.Application.CategoryGroups.CreateCategoryGroup;
using GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;

namespace GameAtlas.Api.CategoryGroups;

internal static class CategoryGroupMappingExtensions
{
	internal static CreateCategoryGroup ToCommand(this CreateCategoryGroupRequest request, Guid gameId) =>
		new(gameId, request.Name, request.Order);

	internal static UpdateCategoryGroup ToCommand(this UpdateCategoryGroupRequest request, Guid gameId, Guid id) =>
		new(gameId, id, request.Name, request.Order);
}
