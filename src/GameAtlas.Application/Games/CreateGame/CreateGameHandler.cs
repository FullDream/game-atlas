using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.CreateGame;

public sealed class CreateGameHandler(IApplicationDbContext context, IMediator mediator) : ICommandHandler<CreateGame, Result<GameDto>>
{
	public async ValueTask<Result<GameDto>> Handle(CreateGame command, CancellationToken cancellationToken)
	{
		var result = Game.Create(command.Name, command.Slug, command.CoverId, command.BannerId, command.ReleaseDate, command.Developer, command.Description);

		if (result.IsFailure)
			return result.Errors;

		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var image = await context.FindImageAsync(command.CoverId!.Value, cancellationToken);

		if (image.IsFailure)
			return image.Errors;

		if (command.BannerId is { } bannerId)
		{
			var banner = await context.FindImageAsync(bannerId, cancellationToken);

			if (banner.IsFailure)
				return banner.Errors;
		}

		var game = result.Value;

		if (await context.Games.AnyAsync(existing => existing.Slug == game.Slug, cancellationToken))
			return CreateGameErrors.SlugConflict;

		context.Games.Add(game);
		await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return await mediator.Send(new GetGame.GetGame(game.Id), cancellationToken);
	}
}
