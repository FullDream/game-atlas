using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.Groups.UpdateCategoryGroup;

public sealed class UpdateCategoryGroupHandler(IApplicationDbContext context) : ICommandHandler<UpdateCategoryGroup, Result<CategoryGroupDto>>
{
	public async ValueTask<Result<CategoryGroupDto>> Handle(UpdateCategoryGroup command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		var siblings = await context.CategoryGroups.AsTracking().ForMap(context, command.GameId, command.MapId)
			.OrderBy(group => group.Order).ThenBy(group => group.CreatedAt).ThenBy(group => group.Id).ToListAsync(cancellationToken);

		var group = siblings.SingleOrDefault(item => item.Id == command.Id);

		if (group is null)
			return UpdateCategoryGroupErrors.NotFound;

		var nextName = command.Name.GetValueOrDefault(group.Name);

		if (nextName is not null && siblings.Any(other => other.Id != group.Id && other.NormalizedName == CategoryGroup.NormalizeName(nextName)))
			return UpdateCategoryGroupErrors.NameConflict;

		var icon = await context.FindOptionalImageAsync(command.IconAssetId.GetValueOrDefault(group.IconAssetId), cancellationToken);

		if (icon.IsFailure)
			return icon.Errors;

		var oldSequence = siblings.Select(item => item.Id).ToArray();
		siblings.Remove(group);

		var placement = CategoryPlacement.GetIndex(siblings.Select(item => item.Id).ToArray(), group.Id, command.BeforeId.GetValueOrDefault(null));

		if (command.BeforeId.IsSpecified && placement.IsFailure)
			return placement.Errors;

		var sequenceChanged = command.BeforeId.IsSpecified && Array.IndexOf(oldSequence, group.Id) != placement.Value;

		var nextOrder = sequenceChanged ? placement.Value + 1 : group.Order;

		var result = group.Update(command.Name, nextOrder, command.IconAssetId, command.Color);

		if (result.IsFailure)
			return result.Errors;

		if (command.BeforeId.IsSpecified)
		{
			siblings.Insert(placement.Value, group);

			if (sequenceChanged)
			{
				for (var index = 0; index < siblings.Count; index++)
					siblings[index].SetDisplayOrder(index + 1);

				group.PlaceAt(placement.Value + 1);
			}
		}

		if (result.Value || sequenceChanged)
			await context.SaveChangesAsync(cancellationToken);

		var tree = await context.ReadCategoryTreeAsync(command.MapId, cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return tree.Single(item => item.Id == group.Id);
	}
}
