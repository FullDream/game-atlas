using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.UpdateGame;

public sealed class UpdateGameHandler(IApplicationDbContext context) : ICommandHandler<UpdateGame, Result<GameDto>>
{
	public async ValueTask<Result<GameDto>> Handle(UpdateGame command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var game = await context.Games.AsTracking().Include(game => game.Image)
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

		var image = game.Image;

		if (command.ImageAssetId.IsSpecified && command.ImageAssetId.Value is { } imageId)
		{
			var selected = await context.FindImageAsync(imageId, cancellationToken);

			if (selected.IsFailure)
				return selected.Errors;

			image = selected.Value;
		}

		var result = game.Update(command.Name, command.Slug, command.ImageAssetId, command.Status);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return game.ToDto(image.ToDto());
	}
}
