using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(IApplicationDbContext context) : ICommandHandler<UpdateCategory, Result<CategoryDto>>
{
	public async ValueTask<Result<CategoryDto>> Handle(UpdateCategory command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		var category = await context.Categories.AsTracking().SingleOrDefaultAsync(category => category.Id == command.Id &&
			category.CategoryGroupId == command.GroupId && context.CategoryGroups.ForMap(context, command.GameId, command.MapId)
				.Any(group => group.Id == category.CategoryGroupId), cancellationToken);

		if (category is null)
			return UpdateCategoryErrors.NotFound;

		var destinationId = command.CategoryGroupId.GetValueOrDefault(category.CategoryGroupId);

		if (!await context.CategoryGroups.ForMap(context, command.GameId, command.MapId).AnyAsync(group => group.Id == destinationId, cancellationToken))
			return UpdateCategoryErrors.GroupNotFound;

		var nextName = command.Name.GetValueOrDefault(category.Name);

		var normalizedName = nextName is null ? null : Category.NormalizeName(nextName);

		if (await context.Categories.AnyAsync(other => other.CategoryGroupId == destinationId && other.Id != category.Id &&
			other.NormalizedName == normalizedName, cancellationToken))
			return UpdateCategoryErrors.NameConflict;

		var icon = await context.FindOptionalImageAsync(command.IconAssetId.GetValueOrDefault(category.IconAssetId), cancellationToken);

		if (icon.IsFailure)
			return icon.Errors;

		var moved = destinationId != category.CategoryGroupId;

		var siblings = await context.Categories.AsTracking()
			.Where(item => item.CategoryGroupId == destinationId)
			.OrderBy(item => item.Order).ThenBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);

		var oldSequence = siblings.Select(item => item.Id).ToArray();
		siblings.Remove(category);

		var placement = CategoryPlacement.GetIndex(siblings.Select(item => item.Id).ToArray(), category.Id, command.BeforeId.GetValueOrDefault(null));

		if (placement.IsFailure)
			return placement.Errors;

		var sequenceChanged = moved || command.BeforeId.IsSpecified && Array.IndexOf(oldSequence, category.Id) != placement.Value;

		var nextOrder = sequenceChanged ? placement.Value + 1 : category.Order;

		var result = category.Update(command.Name, command.IconAssetId, command.Color, nextOrder, command.CategoryGroupId);

		if (result.IsFailure)
			return result.Errors;

		if (command.BeforeId.IsSpecified || moved)
		{
			siblings.Insert(placement.Value, category);

			if (sequenceChanged)
			{
				for (var index = 0; index < siblings.Count; index++)
					siblings[index].SetDisplayOrder(index + 1);

				category.PlaceAt(placement.Value + 1);

				if (moved)
				{
					var source = await context.Categories.AsTracking().Where(item => item.CategoryGroupId == command.GroupId && item.Id != category.Id)
						.OrderBy(item => item.Order).ThenBy(item => item.CreatedAt).ThenBy(item => item.Id).ToArrayAsync(cancellationToken);

					for (var index = 0; index < source.Length; index++)
						source[index].SetDisplayOrder(index + 1);
				}
			}
		}

		if (result.Value || sequenceChanged)
			await context.SaveChangesAsync(cancellationToken);

		var count = await context.Markers.CountAsync(marker => marker.CategoryId == category.Id, cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return category.ToDto(count, icon.Value?.ToDto());
	}
}
