using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(IApplicationDbContext context)
	: ICommandHandler<UpdateCategory, Result<CategoryDto>>
{
	public async ValueTask<Result<CategoryDto>> Handle(UpdateCategory command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var category = await context.Categories.AsTracking().SingleOrDefaultAsync(
			category => category.Id == command.Id && category.CategoryGroupId == command.GroupId &&
				context.CategoryGroups.Any(group =>
					group.Id == category.CategoryGroupId && group.GameId == command.GameId),
			cancellationToken);

		if (category is null)
			return UpdateCategoryErrors.NotFound;

		var destinationId = command.CategoryGroupId.GetValueOrDefault(category.CategoryGroupId);

		if (!await context.CategoryGroups.AnyAsync(
			group => group.Id == destinationId && group.GameId == command.GameId, cancellationToken))
			return UpdateCategoryErrors.GroupNotFound;

		var nextName = command.Name.GetValueOrDefault(category.Name);
		var normalizedName = nextName is null ? null : Category.NormalizeName(nextName);

		if (destinationId != category.CategoryGroupId || normalizedName != category.NormalizedName)
		{
			var nameExists = await context.Categories.AnyAsync(other => other.CategoryGroupId == destinationId &&
				other.Id != command.Id && other.NormalizedName == normalizedName, cancellationToken);

			if (nameExists)
				return UpdateCategoryErrors.NameConflict;
		}

		var result = category.Update(command.Name, command.Icon, command.Color, command.Order, command.CategoryGroupId);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		var markersCount = await context.Markers.CountAsync(marker => marker.CategoryId == category.Id,
			cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return category.ToDto(markersCount);
	}
}
