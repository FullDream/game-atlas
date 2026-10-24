using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.CategoryGroups;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.CreateCategory;

public sealed class CreateCategoryHandler(IApplicationDbContext context)
	: ICommandHandler<CreateCategory, Result<CategoryDto>>
{
	public async ValueTask<Result<CategoryDto>> Handle(CreateCategory command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.CategoryGroups.ForMap(context, command.GameId, command.MapId).AnyAsync(
			group => group.Id == command.GroupId, cancellationToken))
			return CreateCategoryErrors.GroupNotFound;

		var order = command.Order.GetValueOrDefault(null);

		if (!command.Order.IsSpecified)
		{
			var lastOrder = await context.Categories.Where(category => category.CategoryGroupId == command.GroupId)
				.MaxAsync(category => (int?)category.Order, cancellationToken) ?? 0;

			if (lastOrder == int.MaxValue)
				return CreateCategoryErrors.ExplicitOrderRequired;

			order = lastOrder + 1;
		}

		if (order is null)
			return CategoryErrors.InvalidOrder;

		var result = Category.Create(command.GroupId, command.Name, command.Icon, command.Color, order.Value);

		if (result.IsFailure)
			return result.Errors;

		var category = result.Value;
		var nameExists = await context.Categories.AnyAsync(other => other.CategoryGroupId == command.GroupId &&
			other.NormalizedName == category.NormalizedName, cancellationToken);

		if (nameExists)
			return CreateCategoryErrors.NameConflict;

		context.Categories.Add(category);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return category.ToDto(markersCount: 0);
	}
}
