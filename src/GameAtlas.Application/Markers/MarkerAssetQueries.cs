using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Result;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Markers;

internal static class MarkerAssetQueries
{
	internal static async Task<Result<IReadOnlyDictionary<Guid, MediaAssetDto>>> ResolveAssetsAsync(
		this IApplicationDbContext context, IEnumerable<Guid?>? ids, CancellationToken cancellationToken)
	{
		var requested = ids?.ToArray();

		if (requested is null || requested.Any(id => id is null || id == Guid.Empty))
			return MarkerErrors.InvalidMedia;

		var keys = requested.Select(id => id.GetValueOrDefault()).Distinct().ToArray();

		if (keys.Length == 0)
			return Result<IReadOnlyDictionary<Guid, MediaAssetDto>>.Success(new Dictionary<Guid, MediaAssetDto>());

		var assets = await context.MediaAssets.Where(asset => keys.Contains(asset.Id)).WithPreview()
			.ToArrayAsync(cancellationToken);

		if (assets.Length != keys.Length)
		{
			var existing = assets.Select(asset => asset.Id).ToHashSet();
			var missing = keys.Where(id => !existing.Contains(id)).ToArray();

			return await context.MediaFolders.AnyAsync(folder => missing.Contains(folder.Id), cancellationToken)
				? MediaOperationErrors.IncompatibleAsset
				: MediaOperationErrors.AssetNotFound;
		}

		return Result<IReadOnlyDictionary<Guid, MediaAssetDto>>.Success(
			assets.ToDictionary(asset => asset.Id, asset => asset.ToDto()));
	}
}
