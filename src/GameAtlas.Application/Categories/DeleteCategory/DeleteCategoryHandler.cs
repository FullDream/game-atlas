using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.CategoryGroups;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(IApplicationDbContext context) : ICommandHandler<DeleteCategory, Result>
{
	public async ValueTask<Result> Handle(DeleteCategory command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var category = await context.Categories.AsTracking().SingleOrDefaultAsync(
			category => category.Id == command.Id && category.CategoryGroupId == command.GroupId &&
				context.CategoryGroups.ForMap(context, command.GameId, command.MapId).Any(group =>
					group.Id == category.CategoryGroupId),
			cancellationToken);

		if (category is null)
			return DeleteCategoryErrors.NotFound;

		context.Categories.Remove(category);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Result.Success();
	}
}
