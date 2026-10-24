using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media.ListMedia;

public sealed class ListMediaHandler(IApplicationDbContext context)
	: IQueryHandler<ListMedia, Result<PaginatedResult<MediaItemDto>>>
{
	public async ValueTask<Result<PaginatedResult<MediaItemDto>>> Handle(ListMedia query,
		CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (query.FolderId is { } folderId &&
			!await context.MediaFolders.AnyAsync(folder => folder.Id == folderId, cancellationToken))
			return MediaOperationErrors.FolderNotFound;

		var folders = context.MediaFolders.Where(folder => folder.ParentId == query.FolderId);
		var assets = context.MediaAssets.Where(asset => asset.FolderId == query.FolderId);
		var search = query.Search?.Trim();

		if (!string.IsNullOrEmpty(search))
		{
			var pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

			folders = folders.Where(folder => EF.Functions.Like(folder.Name, pattern, "\\"));
			assets = assets.Where(asset => EF.Functions.Like(asset.Name, pattern, "\\"));
		}

		var folderCount = await folders.CountAsync(cancellationToken);
		var total = folderCount + await assets.CountAsync(cancellationToken);
		var selectedFolders = await folders.OrderBy(folder => folder.Name).ThenBy(folder => folder.Id)
			.Skip(query.Offset).Take(query.Limit)
			.Select(folder => new MediaFolderDto(folder.Id, folder.ParentId, folder.Name,
				context.MediaFolders.Count(child => child.ParentId == folder.Id) +
				context.MediaAssets.Count(asset => asset.FolderId == folder.Id), folder.CreatedAt, folder.UpdatedAt))
			.ToArrayAsync(cancellationToken);

		var selectedAssets = await assets.OrderBy(asset => asset.Name).ThenBy(asset => asset.Id)
			.Skip(Math.Max(0, query.Offset - folderCount)).Take(query.Limit - selectedFolders.Length)
			.WithPreview().ToArrayAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return new PaginatedResult<MediaItemDto>(
			selectedFolders.Cast<MediaItemDto>().Concat(selectedAssets.Select(asset => asset.ToDto())).ToArray(),
			query.Limit, query.Offset, total);
	}
}
