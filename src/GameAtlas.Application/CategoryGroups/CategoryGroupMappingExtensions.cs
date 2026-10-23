using GameAtlas.Domain.CategoryGroups;

namespace GameAtlas.Application.CategoryGroups;

internal static class CategoryGroupMappingExtensions
{
	internal static CategoryGroupDto ToDto(this CategoryGroup group) =>
		new(group.Id, group.GameId, group.Name, group.Order, group.CreatedAt, group.UpdatedAt);
}
