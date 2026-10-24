using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Media.Folders.UpdateMediaFolder;

public sealed class UpdateMediaFolderHandler(IApplicationDbContext context)
	: ICommandHandler<UpdateMediaFolder, Result<MediaFolderDto>>
{
	public async ValueTask<Result<MediaFolderDto>> Handle(UpdateMediaFolder command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var folder = await context.MediaFolders.AsTracking().SingleOrDefaultAsync(item => item.Id == command.Id,
			cancellationToken);

		if (folder is null)
			return MediaOperationErrors.FolderNotFound;

		var destination = command.ParentId.GetValueOrDefault(folder.ParentId);
		var ancestor = destination;
		var visited = new HashSet<Guid> { folder.Id };

		while (ancestor is { } id)
		{
			if (!visited.Add(id))
				return MediaErrors.FolderCycle;

			var parent = await context.MediaFolders.Where(item => item.Id == id)
				.Select(item => new { item.ParentId }).SingleOrDefaultAsync(cancellationToken);

			if (parent is null)
				return MediaOperationErrors.FolderNotFound;

			ancestor = parent.ParentId;
		}

		var nextName = command.Name.GetValueOrDefault(folder.Name)?.Trim();

		if (!string.IsNullOrEmpty(nextName))
		{
			var normalized = MediaFolder.NormalizeName(nextName);

			if (await context.MediaFolders.AnyAsync(other => other.Id != folder.Id &&
				other.ParentId == destination && other.NormalizedName == normalized, cancellationToken))
				return MediaOperationErrors.FolderNameConflict;
		}

		var updated = folder.Update(command.Name, command.ParentId);

		if (updated.IsFailure)
			return updated.Errors;

		if (updated.Value)
			await context.SaveChangesAsync(cancellationToken);

		var count = await context.CountItemsAsync(folder.Id, cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return folder.ToDto(count);
	}
}
