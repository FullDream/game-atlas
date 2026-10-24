using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media.Assets.AdmitMediaAsset;

public sealed class AdmitMediaAssetHandler(IApplicationDbContext context, IMediaStorage storage)
	: ICommandHandler<AdmitMediaAsset, Result<MediaAssetDto>>
{
	public async ValueTask<Result<MediaAssetDto>> Handle(AdmitMediaAsset command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (command.FolderId is { } folderId &&
			!await context.MediaFolders.AnyAsync(folder => folder.Id == folderId, cancellationToken))
			return MediaOperationErrors.FolderNotFound;

		if (command.PreviewAssetId.IsSpecified && command.PreviewAssetId.Value is { } previewId)
		{
			var preview = await context.MediaAssets.SingleOrDefaultAsync(asset => asset.Id == previewId,
				cancellationToken);

			if (preview is null)
				return await context.MediaFolders.AnyAsync(folder => folder.Id == previewId, cancellationToken)
					? MediaOperationErrors.IncompatibleAsset
					: MediaOperationErrors.AssetNotFound;

			if (preview is not ImageAsset)
				return MediaOperationErrors.IncompatibleAsset;
		}

		var created = CreateAsset(command);

		if (created.IsFailure)
			return created.Errors;

		var asset = created.Value;

		context.MediaAssets.Add(asset);
		storage.Promote(command.UploadId, command.AssetId);

		try
		{
			await context.SaveChangesAsync(cancellationToken);
		}
		catch
		{
			storage.DeleteAsset(command.AssetId);

			throw;
		}

		var stored = await context.MediaAssets.AsNoTracking().WithPreview()
			.SingleAsync(item => item.Id == asset.Id, cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return stored.ToDto();
	}

	private static Result<MediaAsset> CreateAsset(AdmitMediaAsset command)
	{
		var inspection = command.Inspection;

		if (inspection.ContentType.StartsWith("image/", StringComparison.Ordinal))
		{
			if (command.PreviewAssetId.IsSpecified)
				return MediaErrors.IncompatibleMetadata;

			var result = ImageAsset.Create(command.AssetId, command.Name, command.FolderId, inspection.ContentType,
				command.ByteLength, inspection.Width, inspection.Height, command.Alt.GetValueOrDefault(""),
				inspection.Placeholder);

			return result.IsFailure ? result.Errors : result.Value;
		}

		if (command.Alt.IsSpecified)
			return MediaErrors.IncompatibleMetadata;

		var video = VideoAsset.Create(command.AssetId, command.Name, command.FolderId, inspection.ContentType,
			command.ByteLength, command.PreviewAssetId.GetValueOrDefault(null));

		return video.IsFailure ? video.Errors : video.Value;
	}
}
