using GameAtlas.Application.Maps.Processing;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.RetryMapStyle;

public sealed class RetryMapStyleHandler(IApplicationDbContext context, IMapProgress progress, IMapMutationLock mutationLock,
	IMapProcessingQueue queue, IMapNotifications notifications)
	: ICommandHandler<RetryMapStyle, Result<MapStyleDto>>
{
	public async ValueTask<Result<MapStyleDto>> Handle(RetryMapStyle command, CancellationToken cancellationToken)
	{
		using var mutationLease = await mutationLock.EnterAsync(cancellationToken);
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var style = await (
			from candidate in context.MapStyles.AsTracking()
			join map in context.Maps.ForMap(command.GameId, command.MapId) on candidate.MapId equals map.Id
			where candidate.Id == command.StyleId
			select candidate
		).Include(style => style.Source).SingleOrDefaultAsync(cancellationToken);

		if (style is null)
			return MapOperationErrors.NotFound;

		var result = style.Retry();

		if (result.IsFailure)
			return result.Errors;

		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		var response = style.ToDto(progress);

		queue.Enqueue(style.MapId, style.Id, style.AttemptId);
		mutationLease.Dispose();
		await notifications.StyleUpdatedAsync(style.MapId, response);

		return response;
	}
}
