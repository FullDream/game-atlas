using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.CreateGame;

public sealed class CreateGameHandler(IApplicationDbContext context) : ICommandHandler<CreateGame, Result<GameDto>>
{
	public async ValueTask<Result<GameDto>> Handle(CreateGame command, CancellationToken cancellationToken)
	{
		var result = Game.Create(command.Name, command.Slug, command.ImageAssetId);

		if (result.IsFailure)
			return result.Errors;

		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var image = await context.FindImageAsync(command.ImageAssetId!.Value, cancellationToken);

		if (image.IsFailure)
			return image.Errors;

		var game = result.Value;

		if (await context.Games.AnyAsync(existing => existing.Slug == game.Slug, cancellationToken))
			return CreateGameErrors.SlugConflict;

		context.Games.Add(game);
		await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return game.ToDto(image.Value.ToDto());
	}
}
