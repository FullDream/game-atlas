using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.ListMaps;

public sealed class ListMapsHandler(IApplicationDbContext context)
	: IQueryHandler<ListMaps, Result<IReadOnlyCollection<MapDto>>>
{
	public async ValueTask<Result<IReadOnlyCollection<MapDto>>> Handle(ListMaps query,
		CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.Games.AnyAsync(game => game.Id == query.GameId, cancellationToken))
			return MapOperationErrors.NotFound;

		var maps = await context.Maps.ForGame(query.GameId).WithStyles()
			.OrderBy(map => map.CreatedAt).ThenBy(map => map.Id).ToArrayAsync(cancellationToken);

		var result = maps.Select(map => map.ToDto()).ToArray();

		await transaction.CommitAsync(cancellationToken);

		return result;
	}
}
