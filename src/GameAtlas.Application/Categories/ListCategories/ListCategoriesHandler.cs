using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.ListCategories;

public sealed class ListCategoriesHandler(IApplicationDbContext context)
	: IQueryHandler<ListCategories, Result<IReadOnlyCollection<CategoryDto>>>
{
	public async ValueTask<Result<IReadOnlyCollection<CategoryDto>>> Handle(
		ListCategories query, CancellationToken cancellationToken)
	{
		if (!await context.CategoryGroups.AnyAsync(
			group => group.Id == query.GroupId && group.GameId == query.GameId, cancellationToken))
			return ListCategoriesErrors.GroupNotFound;

		var categories = await context.Categories.AsNoTracking()
			.Where(category => category.CategoryGroupId == query.GroupId)
			.OrderBy(category => category.Order).ThenBy(category => category.CreatedAt).ThenBy(category => category.Id)
			.ToDto(context.Markers)
			.ToArrayAsync(cancellationToken);

		return categories;
	}
}
