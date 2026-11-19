using GameAtlas.Application.Maps.Processing;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.UpdateMapStyle;

public sealed class UpdateMapStyleHandler(IApplicationDbContext context, IMapProgress progress,
	IMapNotifications notifications)
	: ICommandHandler<UpdateMapStyle, Result<MapStyleDto>>
{
	public async ValueTask<Result<MapStyleDto>> Handle(UpdateMapStyle command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		await context.LockMapAsync(command.MapId, cancellationToken);
		var map = await context.Maps.AsTracking().ForMap(command.GameId, command.MapId).WithStyles()
			.SingleOrDefaultAsync(cancellationToken);

		if (map is null)
			return MapOperationErrors.NotFound;

		var result = map.UpdateStyle(command.StyleId, command.Name, command.BackgroundColor);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);
		var style = map.Styles.Single(style => style.Id == command.StyleId);

		var response = style.ToDto(progress);

		if (result.Value)
			await notifications.StyleUpdatedAsync(style.MapId, response);

		return response;
	}
}
