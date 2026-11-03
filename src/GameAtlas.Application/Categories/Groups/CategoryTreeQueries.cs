using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.Groups;

internal static class CategoryTreeQueries
{
	internal static async Task<CategoryGroupDto[]> ReadCategoryTreeAsync(this IApplicationDbContext context,
		Guid mapId, CancellationToken cancellationToken)
	{
		var groups = await context.CategoryGroups.AsNoTracking().Where(group => group.MapId == mapId)
			.OrderBy(group => group.Order).ThenBy(group => group.CreatedAt).ThenBy(group => group.Id)
			.ToArrayAsync(cancellationToken);

		var categories = await context.Categories.AsNoTracking()
			.Where(category => context.CategoryGroups.Any(group => group.MapId == mapId && group.Id == category.CategoryGroupId))
			.OrderBy(category => category.Order).ThenBy(category => category.CreatedAt).ThenBy(category => category.Id)
			.ToDto(context.Markers, context.MediaAssets).ToArrayAsync(cancellationToken);

		var iconIds = groups.Where(group => group.IconAssetId.HasValue).Select(group => group.IconAssetId!.Value).Distinct().ToArray();

		var images = await context.MediaAssets.OfType<ImageAsset>().AsNoTracking()
			.Where(image => iconIds.Contains(image.Id)).ToDictionaryAsync(image => image.Id, cancellationToken);

		var children = categories.ToLookup(category => category.CategoryGroupId);

		return groups.Select(group => group.ToDto(
			group.IconAssetId is { } iconId && images.TryGetValue(iconId, out var image) ? image.ToDto() : null,
			children[group.Id].ToArray())).ToArray();
	}
}
