using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media.Folders.DeleteMediaFolder;

public sealed class DeleteMediaFolderHandler(IApplicationDbContext context)
	: ICommandHandler<DeleteMediaFolder, Result>
{
	public async ValueTask<Result> Handle(DeleteMediaFolder command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.MediaFolders.AnyAsync(folder => folder.Id == command.Id, cancellationToken))
			return MediaOperationErrors.FolderNotFound;

		if (await context.MediaFolders.AnyAsync(folder => folder.ParentId == command.Id, cancellationToken) ||
			await context.MediaAssets.AnyAsync(asset => asset.FolderId == command.Id, cancellationToken))
			return MediaOperationErrors.FolderNotEmpty;

		await context.MediaFolders.Where(folder => folder.Id == command.Id).ExecuteDeleteAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Result.Success();
	}
}
