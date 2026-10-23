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
		if (!await context.Games.AnyAsync(game => game.Id == query.GameId, cancellationToken))
			return ListCategoryGroupsErrors.GameNotFound;

		var groups = await context
			.CategoryGroups
			.AsNoTracking()
			.Where(group => group.GameId == query.GameId)
			.OrderBy(group => group.Order)
			.ThenBy(group => group.CreatedAt)
			.ThenBy(group => group.Id)
			.ToArrayAsync(cancellationToken);

		return groups.Select(group => group.ToDto()).ToArray();
	}
}
