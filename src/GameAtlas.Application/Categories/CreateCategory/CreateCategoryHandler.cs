using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.CreateCategory;

public sealed class CreateCategoryHandler(IApplicationDbContext context) : ICommandHandler<CreateCategory, Result<CategoryDto>>
{
	public async ValueTask<Result<CategoryDto>> Handle(CreateCategory command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.CategoryGroups.ForMap(context, command.GameId, command.MapId).AnyAsync(group => group.Id == command.GroupId, cancellationToken))
			return CreateCategoryErrors.GroupNotFound;

		var siblings = await context.Categories.AsTracking().Where(category => category.CategoryGroupId == command.GroupId)
			.OrderBy(category => category.Order).ThenBy(category => category.CreatedAt).ThenBy(category => category.Id).ToListAsync(cancellationToken);

		var result = Category.Create(command.GroupId, command.Name, command.IconAssetId, command.Color, siblings.Count + 1);

		if (result.IsFailure)
			return result.Errors;

		var category = result.Value;

		var placement = CategoryPlacement.GetIndex(siblings.Select(item => item.Id).ToArray(), category.Id, command.BeforeId.GetValueOrDefault(null));

		if (placement.IsFailure)
			return placement.Errors;

		if (siblings.Any(other => other.NormalizedName == category.NormalizedName))
			return CreateCategoryErrors.NameConflict;

		var icon = await context.FindOptionalImageAsync(command.IconAssetId, cancellationToken);

		if (icon.IsFailure)
			return icon.Errors;

		siblings.Insert(placement.Value, category);

		for (var index = 0; index < siblings.Count; index++)
			siblings[index].SetDisplayOrder(index + 1);

		context.Categories.Add(category);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return category.ToDto(0, icon.Value?.ToDto());
	}
}
