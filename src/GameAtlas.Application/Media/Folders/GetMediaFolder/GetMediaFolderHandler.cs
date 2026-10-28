using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media.Folders.GetMediaFolder;

public sealed class GetMediaFolderHandler(IApplicationDbContext context)
	: IQueryHandler<GetMediaFolder, Result<MediaFolderDetailsDto>>
{
	public async ValueTask<Result<MediaFolderDetailsDto>> Handle(GetMediaFolder query,
		CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		var currentFolder = await context.MediaFolders.Where(folder => folder.Id == query.Id)
			.Select(folder => new MediaFolderDto(folder.Id, folder.ParentId, folder.Name,
				context.MediaFolders.Count(child => child.ParentId == folder.Id) +
				context.MediaAssets.Count(asset => asset.FolderId == folder.Id), folder.CreatedAt, folder.UpdatedAt))
			.SingleOrDefaultAsync(cancellationToken);

		if (currentFolder is null)
			return MediaOperationErrors.FolderNotFound;

		var ancestors = new List<MediaFolderBreadcrumbDto>();
		var parentId = currentFolder.ParentId;

		while (parentId.HasValue)
		{
			var parent = await context.MediaFolders.Where(folder => folder.Id == parentId)
				.Select(folder => new { folder.Id, folder.Name, folder.ParentId })
				.SingleAsync(cancellationToken);

			ancestors.Add(new MediaFolderBreadcrumbDto(parent.Id, parent.Name));
			parentId = parent.ParentId;
		}

		ancestors.Reverse();

		await transaction.CommitAsync(cancellationToken);

		return new MediaFolderDetailsDto(currentFolder, ancestors);
	}
}
