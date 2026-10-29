using GameAtlas.Application.Maps.Processing;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.DeleteGame;

public sealed class DeleteGameHandler(IApplicationDbContext context, IMapCleanup cleanup, IMapMutationLock mutationLock,
	IMapNotifications notifications)
	: ICommandHandler<DeleteGame, Result>
{
	public async ValueTask<Result> Handle(DeleteGame command, CancellationToken cancellationToken)
	{
		using var mutationLease = await mutationLock.EnterAsync(cancellationToken);
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var mapIds = await context.Maps.Where(map => map.GameId == command.Id).Select(map => map.Id)
			.ToArrayAsync(cancellationToken);

		foreach (var mapId in mapIds)
			cleanup.Schedule(mapId);

		var deleted = await context.Games.Where(game => game.Id == command.Id)
			.ExecuteDeleteAsync(cancellationToken);

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		cleanup.Dispatch();
		mutationLease.Dispose();

		foreach (var mapId in mapIds)
			await notifications.ProcessingUpdatedAsync(new MapProcessingDto(mapId, null, null, null, 0, null, Deleted: true));

		return deleted == 0 ? Result.Failure(DeleteGameErrors.NotFound) : Result.Success();
	}
}
