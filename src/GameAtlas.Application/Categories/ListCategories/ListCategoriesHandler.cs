using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.CategoryGroups;
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
		if (!await context.CategoryGroups.ForMap(context, query.GameId, query.MapId).AnyAsync(
			group => group.Id == query.GroupId, cancellationToken))
			return ListCategoriesErrors.GroupNotFound;

		var categories = await context.Categories.AsNoTracking()
			.Where(category => category.CategoryGroupId == query.GroupId)
			.OrderBy(category => category.Order).ThenBy(category => category.CreatedAt).ThenBy(category => category.Id)
			.ToDto(context.Markers, context.MediaAssets)
			.ToArrayAsync(cancellationToken);

		return categories;
	}
}
