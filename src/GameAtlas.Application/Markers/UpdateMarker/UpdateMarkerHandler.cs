using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories;
using GameAtlas.Application.Markers.Media;
using GameAtlas.Domain;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Markers.UpdateMarker;

public sealed class UpdateMarkerHandler(IApplicationDbContext context)
	: ICommandHandler<UpdateMarker, Result<MarkerDto>>
{
	public async ValueTask<Result<MarkerDto>> Handle(UpdateMarker command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var marker = await context.Markers.AsTracking().ForMap(context, command.GameId, command.MapId)
			.Include(item => item.Media).SingleOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

		if (marker is null)
			return UpdateMarkerErrors.NotFound;

		if (command.CategoryId.IsSpecified)
		{
			var categoryExists = await context.Categories.ForMap(context, command.GameId, command.MapId)
				.AnyAsync(category => category.Id == command.CategoryId.Value, cancellationToken);

			if (!categoryExists)
				return UpdateMarkerErrors.CategoryNotFound;
		}

		var position = command.Position.IsSpecified
			? command.Position.Value.Merge(marker.Position)
			: Result<Position>.Success(marker.Position);

		if (position.IsFailure)
			return position.Errors;

		Optional<IReadOnlyCollection<MarkerMedia?>?> replacement = default;

		if (command.Media.IsSpecified)
		{
			var media = command.Media.Value.ToDomain();

			if (media.IsFailure)
				return media.Errors;

			replacement = Optional<IReadOnlyCollection<MarkerMedia?>?>.From(media.Value);
		}

		var result = marker.Update(command.CategoryId, command.Name, command.Description, position.Value, replacement);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return marker.ToDto();
	}
}
