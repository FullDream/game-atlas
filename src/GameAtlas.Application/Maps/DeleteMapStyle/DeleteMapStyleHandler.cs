using GameAtlas.Application.Maps.Processing;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.DeleteMapStyle;

public sealed class DeleteMapStyleHandler(IApplicationDbContext context, IMapCleanup cleanup,
	IMapNotifications notifications)
	: ICommandHandler<DeleteMapStyle, Result>
{
	public async ValueTask<Result> Handle(DeleteMapStyle command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		await context.LockMapAsync(command.MapId, cancellationToken);
		var map = await context.Maps.AsTracking().ForMap(command.GameId, command.MapId).WithStyles()
			.SingleOrDefaultAsync(cancellationToken);

		if (map is null)
			return MapOperationErrors.NotFound;

		var result = map.DeleteStyle(command.StyleId);

		if (result.IsFailure)
			return result;

		cleanup.Schedule(map.Id, command.StyleId);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		cleanup.Dispatch();
		await notifications.ProcessingUpdatedAsync(new MapProcessingDto(map.Id, command.StyleId, null, null, 0, null,
			Deleted: true));

		return Result.Success();
	}
}
