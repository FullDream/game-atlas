using GameAtlas.Application.Maps.Processing;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.CreateMap;

public sealed class CreateMapHandler(IApplicationDbContext context, IMapMutationLock mutationLock) : ICommandHandler<CreateMap, Result<MapDto>>
{
	public async ValueTask<Result<MapDto>> Handle(CreateMap command, CancellationToken cancellationToken)
	{
		using var mutationLease = await mutationLock.EnterAsync(cancellationToken);
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.Games.AnyAsync(game => game.Id == command.GameId, cancellationToken))
			return MapOperationErrors.NotFound;

		var created = Map.Create(command.GameId, command.Name);

		if (created.IsFailure)
			return created.Errors;

		var map = created.Value;
		var nameExists = await context.Maps.ForGame(command.GameId)
			.AnyAsync(other => other.NormalizedName == map.NormalizedName, cancellationToken);

		if (nameExists)
			return MapOperationErrors.NameConflict;

		context.Maps.Add(map);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return map.ToDto();
	}
}
