using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.CategoryGroups.CreateCategoryGroup;

public sealed class CreateCategoryGroupHandler(IApplicationDbContext context)
	: ICommandHandler<CreateCategoryGroup, Result<CategoryGroupDto>>
{
	public async ValueTask<Result<CategoryGroupDto>> Handle(
		CreateCategoryGroup command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.Games.AnyAsync(game => game.Id == command.GameId, cancellationToken))
			return CreateCategoryGroupErrors.GameNotFound;

		var order = command.Order.GetValueOrDefault(null);

		if (!command.Order.IsSpecified)
		{
			var lastOrder = await context.CategoryGroups.Where(group => group.GameId == command.GameId)
				.MaxAsync(group => (int?)group.Order, cancellationToken) ?? 0;

			if (lastOrder == int.MaxValue)
				return CreateCategoryGroupErrors.ExplicitOrderRequired;

			order = lastOrder + 1;
		}

		if (order is null)
			return CategoryGroupErrors.InvalidOrder;

		var result = CategoryGroup.Create(command.GameId, command.Name, order.Value);

		if (result.IsFailure)
			return result.Errors;

		var group = result.Value;
		var nameExists = await context.CategoryGroups.AnyAsync(
			other => other.GameId == command.GameId && other.NormalizedName == group.NormalizedName, cancellationToken);

		if (nameExists)
			return CreateCategoryGroupErrors.NameConflict;

		context.CategoryGroups.Add(group);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return group.ToDto();
	}
}
