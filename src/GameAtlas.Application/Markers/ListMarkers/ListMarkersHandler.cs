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
		if (!await context.Games.AnyAsync(game => game.Id == query.GameId, cancellationToken))
			return ListMarkersErrors.NotFound;

		var markers = await context.Markers.AsNoTracking().ForGame(context, query.GameId)
			.OrderBy(marker => marker.CreatedAt).ThenBy(marker => marker.Id)
			.Include(marker => marker.Media).ToArrayAsync(cancellationToken);

		return markers.Select(marker => marker.ToDto()).ToArray();
	}
}
