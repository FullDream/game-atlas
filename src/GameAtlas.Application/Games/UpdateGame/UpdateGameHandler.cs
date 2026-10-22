using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.UpdateGame;

public sealed class UpdateGameHandler(IApplicationDbContext context) : ICommandHandler<UpdateGame, Result<GameDto>>
{
	public async ValueTask<Result<GameDto>> Handle(UpdateGame command, CancellationToken cancellationToken)
	{
		var game = await context.Games.AsTracking()
			.SingleOrDefaultAsync(game => game.Id == command.Id, cancellationToken);

		if (game is null)
			return UpdateGameErrors.NotFound;

		var slug = command.Slug.GetValueOrDefault(game.Slug)?.Trim();

		if (slug != game.Slug)
		{
			var slugExists = await context.Games.AnyAsync(
				other => other.Id != command.Id && other.Slug == slug, cancellationToken);

			if (slugExists)
				return UpdateGameErrors.SlugConflict;
		}

		var result = game.Update(command.Name, command.Slug, command.Image, command.MapId, command.Status);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		return game.ToDto();
	}
}
