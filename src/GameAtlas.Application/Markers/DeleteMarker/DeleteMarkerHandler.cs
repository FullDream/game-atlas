using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Markers.DeleteMarker;

public sealed class DeleteMarkerHandler(IApplicationDbContext context) : ICommandHandler<DeleteMarker, Result>
{
	public async ValueTask<Result> Handle(DeleteMarker command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var marker = await context.Markers.AsTracking().ForMap(context, command.GameId, command.MapId)
			.SingleOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

		if (marker is null)
			return DeleteMarkerErrors.NotFound;

		context.Markers.Remove(marker);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Result.Success();
	}
}
