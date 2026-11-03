using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.Groups.ListCategoryGroups;

public sealed class ListCategoryGroupsHandler(IApplicationDbContext context)
	: IQueryHandler<ListCategoryGroups, Result<IReadOnlyCollection<CategoryGroupDto>>>
{
	public async ValueTask<Result<IReadOnlyCollection<CategoryGroupDto>>> Handle(ListCategoryGroups query, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.Maps.AnyAsync(map => map.Id == query.MapId && map.GameId == query.GameId, cancellationToken))
			return ListCategoryGroupsErrors.MapNotFound;

		var result = await context.ReadCategoryTreeAsync(query.MapId, cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return result;
	}
}
