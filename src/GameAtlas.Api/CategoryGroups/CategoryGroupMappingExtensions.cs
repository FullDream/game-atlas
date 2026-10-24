using GameAtlas.Application.CategoryGroups.CreateCategoryGroup;
using GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;

namespace GameAtlas.Api.CategoryGroups;

internal static class CategoryGroupMappingExtensions
{
	internal static CreateCategoryGroup ToCommand(this CreateCategoryGroupRequest request, Guid gameId, Guid mapId) =>
		new(gameId, mapId, request.Name, request.Order);

	internal static UpdateCategoryGroup ToCommand(this UpdateCategoryGroupRequest request, Guid gameId,
		Guid mapId, Guid id) =>
		new(gameId, mapId, id, request.Name, request.Order);
}
