using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.CategoryGroups.DeleteCategoryGroup;

public sealed class DeleteCategoryGroupHandler(IApplicationDbContext context)
	: ICommandHandler<DeleteCategoryGroup, Result>
{
	public async ValueTask<Result> Handle(DeleteCategoryGroup command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var group = await context.CategoryGroups.AsTracking().SingleOrDefaultAsync(
			group => group.GameId == command.GameId && group.Id == command.Id, cancellationToken);

		if (group is null)
			return DeleteCategoryGroupErrors.NotFound;

		context.CategoryGroups.Remove(group);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Result.Success();
	}
}
