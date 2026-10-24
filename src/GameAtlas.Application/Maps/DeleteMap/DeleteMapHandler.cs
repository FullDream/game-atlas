using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.DeleteMap;

public sealed class DeleteMapHandler(IApplicationDbContext context, IMapCleanup cleanup)
	: ICommandHandler<DeleteMap, Result>
{
	public async ValueTask<Result> Handle(DeleteMap command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var mapId = await context.Maps.ForMap(command.GameId, command.MapId).Select(map => (Guid?)map.Id)
			.SingleOrDefaultAsync(cancellationToken);

		if (mapId is null)
			return MapOperationErrors.NotFound;

		cleanup.Schedule(mapId.Value);
		await context.Maps.ForMap(command.GameId, command.MapId).ExecuteDeleteAsync(cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Result.Success();
	}
}
