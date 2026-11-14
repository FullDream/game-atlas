using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Processing;
using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.PublishMap;

public sealed class PublishMapHandler(
	IApplicationDbContext context,
	IMapProgress progress,
	IMapMutationLock mutationLock) : ICommandHandler<PublishMap, Result<MapDto>>
{
	public async ValueTask<Result<MapDto>> Handle(PublishMap command, CancellationToken cancellationToken)
	{
		using var lease = await mutationLock.EnterAsync(cancellationToken);
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		var map = await context.Maps.AsTracking().ForMap(command.GameId, command.MapId).WithStyles()
			.SingleOrDefaultAsync(cancellationToken);

		if (map is null)
			return MapOperationErrors.NotFound;

		var draftGroups = await context.CategoryGroups.AsTracking()
			.Where(group => group.MapId == map.Id && group.Status == PublicationStatus.Draft)
			.ToListAsync(cancellationToken);

		var draftCategories = await (from category in context.Categories.AsTracking()
									 join groupItem in context.CategoryGroups on category.CategoryGroupId equals groupItem.Id
									 where groupItem.MapId == map.Id && category.Status == PublicationStatus.Draft
									 select category).ToListAsync(cancellationToken);

		var draftMarkers = await (from marker in context.Markers.AsTracking()
								  join category in context.Categories on marker.CategoryId equals category.Id
								  join groupItem in context.CategoryGroups on category.CategoryGroupId equals groupItem.Id
								  where groupItem.MapId == map.Id && marker.Status == PublicationStatus.Draft
								  select marker).ToListAsync(cancellationToken);

		var changed = map.Publish();

		foreach (var group in draftGroups)
			changed |= group.Publish();

		foreach (var category in draftCategories)
			changed |= category.Publish();

		foreach (var marker in draftMarkers)
			changed |= marker.Publish();

		if (changed)
			await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return map.ToDto(progress);
	}
}
