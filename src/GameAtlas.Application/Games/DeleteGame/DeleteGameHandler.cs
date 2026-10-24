using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.DeleteGame;

public sealed class DeleteGameHandler(IApplicationDbContext context, IMapCleanup cleanup)
	: ICommandHandler<DeleteGame, Result>
{
	public async ValueTask<Result> Handle(DeleteGame command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var mapIds = await context.Maps.Where(map => map.GameId == command.Id).Select(map => map.Id)
			.ToArrayAsync(cancellationToken);

		foreach (var mapId in mapIds)
			cleanup.Schedule(mapId);

		var deleted = await context.Games.Where(game => game.Id == command.Id)
			.ExecuteDeleteAsync(cancellationToken);

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return deleted == 0 ? Result.Failure(DeleteGameErrors.NotFound) : Result.Success();
	}
}
