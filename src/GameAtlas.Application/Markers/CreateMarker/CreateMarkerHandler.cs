using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Markers.CreateMarker;

public sealed class CreateMarkerHandler(IApplicationDbContext context)
	: ICommandHandler<CreateMarker, Result<MarkerDto>>
{
	public async ValueTask<Result<MarkerDto>> Handle(CreateMarker command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		var categoryExists = await context.Categories.ForMap(context, command.GameId, command.MapId)
			.AnyAsync(category => category.Id == command.CategoryId, cancellationToken);

		if (!categoryExists)
			return CreateMarkerErrors.CategoryNotFound;

		var position = command.Position.ToDomain();

		if (position.IsFailure)
			return position.Errors;

		var media = await context.ResolveAssetsAsync(command.Media, cancellationToken);

		if (media.IsFailure)
			return media.Errors;

		if (command.CategoryId is not { } categoryId)
			return MarkerErrors.CategoryRequired;

		var result = Marker.Create(categoryId, command.Name, position.Value, command.Description, command.Media);

		if (result.IsFailure)
			return result.Errors;

		context.Markers.Add(result.Value);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return result.Value.ToDto(media.Value);
	}
}
