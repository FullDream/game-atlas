using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media.Folders.CreateMediaFolder;

public sealed class CreateMediaFolderHandler(IApplicationDbContext context)
	: ICommandHandler<CreateMediaFolder, Result<MediaFolderDto>>
{
	public async ValueTask<Result<MediaFolderDto>> Handle(CreateMediaFolder command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (command.ParentId is { } parent &&
			!await context.MediaFolders.AnyAsync(folder => folder.Id == parent, cancellationToken))
			return MediaOperationErrors.FolderNotFound;

		var created = MediaFolder.Create(command.Name, command.ParentId);

		if (created.IsFailure)
			return created.Errors;

		var folder = created.Value;

		if (await context.MediaFolders.AnyAsync(other => other.ParentId == folder.ParentId &&
			other.NormalizedName == folder.NormalizedName, cancellationToken))
			return MediaOperationErrors.FolderNameConflict;

		context.MediaFolders.Add(folder);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return folder.ToDto(0);
	}
}
