using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.DeleteGame;

public sealed class DeleteGameHandler(IApplicationDbContext context) : ICommandHandler<DeleteGame, Result>
{
	public async ValueTask<Result> Handle(DeleteGame command, CancellationToken cancellationToken)
	{
		var deleted = await context.Games.Where(game => game.Id == command.Id)
			.ExecuteDeleteAsync(cancellationToken);

		return deleted == 0 ? Result.Failure(DeleteGameErrors.NotFound) : Result.Success();
	}
}
