using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.CategoryGroups.ListCategoryGroups;

public sealed class ListCategoryGroupsHandler(IApplicationDbContext context)
	: IQueryHandler<ListCategoryGroups, Result<IReadOnlyCollection<CategoryGroupDto>>>
{
	public async ValueTask<Result<IReadOnlyCollection<CategoryGroupDto>>> Handle(
		ListCategoryGroups query,
		CancellationToken cancellationToken)
	{
		if (!await context.Maps.AnyAsync(map => map.Id == query.MapId && map.GameId == query.GameId, cancellationToken))
			return ListCategoryGroupsErrors.MapNotFound;

		var groups = await context
			.CategoryGroups
			.AsNoTracking()
			.Where(group => group.MapId == query.MapId)
			.OrderBy(group => group.Order)
			.ThenBy(group => group.CreatedAt)
			.ThenBy(group => group.Id)
			.ToArrayAsync(cancellationToken);

		return groups.Select(group => group.ToDto()).ToArray();
	}
}
