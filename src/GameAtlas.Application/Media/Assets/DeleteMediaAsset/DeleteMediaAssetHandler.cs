using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media.Assets.DeleteMediaAsset;

public sealed class DeleteMediaAssetHandler(IApplicationDbContext context, IMediaStorage storage)
	: ICommandHandler<DeleteMediaAsset, Result>
{
	public async ValueTask<Result> Handle(DeleteMediaAsset command, CancellationToken cancellationToken)
	{
		await using (var transaction = await context.BeginTransactionAsync(cancellationToken))
		{
			if (!await context.MediaAssets.AnyAsync(asset => asset.Id == command.Id, cancellationToken))
				return MediaOperationErrors.AssetNotFound;

			if (await context.Games.AnyAsync(game => (game.CoverId == command.Id || game.BannerId == command.Id), cancellationToken) ||
				await context.CategoryGroups.AnyAsync(group => group.IconAssetId == command.Id, cancellationToken) ||
				await context.Categories.AnyAsync(category => category.IconAssetId == command.Id, cancellationToken) ||
				await context.MapStyles.AnyAsync(style => style.SourceAssetId == command.Id, cancellationToken) ||
				await context.Markers.AnyAsync(marker => marker.Media.Any(item => item.AssetId == command.Id), cancellationToken) ||
				await context.MediaAssets.OfType<VideoAsset>()
					.AnyAsync(video => video.PreviewAssetId == command.Id, cancellationToken))
				return MediaOperationErrors.AssetInUse;

			storage.DeleteAsset(command.Id);
			await context.MediaAssets.Where(asset => asset.Id == command.Id).ExecuteDeleteAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		}

		return Result.Success();
	}
}
