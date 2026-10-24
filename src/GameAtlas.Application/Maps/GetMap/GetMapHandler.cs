using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.GetMap;

public sealed class GetMapHandler(IApplicationDbContext context) : IQueryHandler<GetMap, Result<MapDto>>
{
	public async ValueTask<Result<MapDto>> Handle(GetMap query, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var map = await context.Maps.ForMap(query.GameId,
			query.MapId).WithStyles().SingleOrDefaultAsync(cancellationToken);

		Result<MapDto> result = map is null ? MapOperationErrors.NotFound : map.ToDto();

		await transaction.CommitAsync(cancellationToken);

		return result;
	}
}
