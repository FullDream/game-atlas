using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Processing;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.UpdateMap;

public sealed class UpdateMapHandler(IApplicationDbContext context, IMapProgress progress, IMapMutationLock mutationLock)
	: ICommandHandler<UpdateMap, Result<MapDto>>
{
	public async ValueTask<Result<MapDto>> Handle(UpdateMap command, CancellationToken cancellationToken)
	{
		using var lease = await mutationLock.EnterAsync(cancellationToken);
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		var map = await context.Maps.AsTracking().ForMap(command.GameId, command.MapId).WithStyles()
			.SingleOrDefaultAsync(cancellationToken);

		if (map is null)
			return MapOperationErrors.NotFound;

		var name = command.Name.GetValueOrDefault(map.Name);

		if (name is not null && await context.Maps.ForGame(command.GameId)
			.AnyAsync(other => other.Id != command.MapId && other.NormalizedName == Map.NormalizeName(name), cancellationToken))
			return MapOperationErrors.NameConflict;

		var result = map.Rename(name);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return map.ToDto(progress);
	}
}
