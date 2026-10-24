using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Markers.ListMarkers;

public sealed class ListMarkersHandler(IApplicationDbContext context)
	: IQueryHandler<ListMarkers, Result<IReadOnlyCollection<MarkerDto>>>
{
	public async ValueTask<Result<IReadOnlyCollection<MarkerDto>>> Handle(
		ListMarkers query, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.Maps.AnyAsync(map => map.Id == query.MapId && map.GameId == query.GameId, cancellationToken))
			return ListMarkersErrors.NotFound;

		var markers = await context.Markers.AsNoTracking().ForMap(context, query.GameId, query.MapId)
			.OrderBy(marker => marker.CreatedAt).ThenBy(marker => marker.Id)
			.Include(marker => marker.Media).ToArrayAsync(cancellationToken);

		var ids = markers.SelectMany(marker => marker.Media).Select(item => (Guid?)item.AssetId).Distinct();
		var assets = await context.ResolveAssetsAsync(ids, cancellationToken);

		if (assets.IsFailure)
			return assets.Errors;

		await transaction.CommitAsync(cancellationToken);

		return markers.Select(marker => marker.ToDto(assets.Value)).ToArray();
	}
}
