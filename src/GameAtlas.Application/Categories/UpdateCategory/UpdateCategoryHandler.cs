using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.CategoryGroups;
using GameAtlas.Application.Media;
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
				context.CategoryGroups.ForMap(context, command.GameId, command.MapId).Any(group =>
					group.Id == category.CategoryGroupId),
			cancellationToken);

		if (category is null)
			return UpdateCategoryErrors.NotFound;

		var destinationId = command.CategoryGroupId.GetValueOrDefault(category.CategoryGroupId);

		if (!await context.CategoryGroups.ForMap(context, command.GameId, command.MapId).AnyAsync(
			group => group.Id == destinationId, cancellationToken))
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

		var iconId = command.IconAssetId.GetValueOrDefault(category.IconAssetId);
		var icon = await context.FindImageAsync(iconId ?? Guid.Empty, cancellationToken);

		if (icon.IsFailure)
			return icon.Errors;

		var result = category.Update(command.Name, command.IconAssetId, command.Color, command.Order,
			command.CategoryGroupId);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		var markersCount = await context.Markers.CountAsync(marker => marker.CategoryId == category.Id,
			cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return category.ToDto(markersCount, icon.Value.ToDto());
	}
}
