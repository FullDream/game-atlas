using GameAtlas.Domain.Categories;
using GameAtlas.Application.Media;

namespace GameAtlas.Application.Categories.Groups;

internal static class CategoryGroupMappingExtensions
{
	internal static CategoryGroupDto ToDto(this CategoryGroup group, ImageAssetDto? icon = null, IReadOnlyCollection<CategoryDto>? categories = null) =>
		new(group.Id, group.MapId, group.Name, group.Color, group.CreatedAt, group.UpdatedAt, icon, categories?.Sum(category => category.MarkersCount) ?? 0, categories ?? []);
}
