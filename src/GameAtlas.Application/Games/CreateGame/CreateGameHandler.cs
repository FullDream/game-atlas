using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.CreateGame;

public sealed class CreateGameHandler(IApplicationDbContext context) : ICommandHandler<CreateGame, Result<GameDto>>
{
	public async ValueTask<Result<GameDto>> Handle(CreateGame command, CancellationToken cancellationToken)
	{
		var result = Game.Create(command.Name, command.Slug, command.Image, command.MapId);

		if (result.IsFailure)
			return result.Errors;

		var game = result.Value;

		if (await context.Games.AnyAsync(existing => existing.Slug == game.Slug, cancellationToken))
			return CreateGameErrors.SlugConflict;

		context.Games.Add(game);
		await context.SaveChangesAsync(cancellationToken);

		return game.ToDto();
	}
}
