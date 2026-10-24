using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media;

public static class MediaReferenceExtensions
{
	public static async Task<Result<ImageAsset>> FindImageAsync(this IApplicationDbContext context, Guid id,
		CancellationToken cancellationToken)
	{
		var asset = await context.MediaAssets.SingleOrDefaultAsync(asset => asset.Id == id, cancellationToken);

		if (asset is ImageAsset image)
			return image;

		if (asset is not null ||
			await context.MediaFolders.AnyAsync(folder => folder.Id == id, cancellationToken))
			return MediaOperationErrors.IncompatibleAsset;

		return MediaOperationErrors.AssetNotFound;
	}
}
