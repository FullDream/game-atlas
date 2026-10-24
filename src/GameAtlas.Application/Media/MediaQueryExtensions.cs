using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media;

public static class MediaQueryExtensions
{
	public static IQueryable<MediaAsset> WithPreview(this IQueryable<MediaAsset> assets) =>
		assets.Include(asset => ((VideoAsset)asset).Preview);

	internal static async Task<int> CountItemsAsync(this IApplicationDbContext context, Guid folderId,
		CancellationToken cancellationToken) =>
		await context.MediaFolders.CountAsync(folder => folder.ParentId == folderId, cancellationToken) +
		await context.MediaAssets.CountAsync(asset => asset.FolderId == folderId, cancellationToken);
}
