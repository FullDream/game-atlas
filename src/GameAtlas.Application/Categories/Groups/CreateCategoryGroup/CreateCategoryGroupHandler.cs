using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Categories.Groups.CreateCategoryGroup;

public sealed class CreateCategoryGroupHandler(IApplicationDbContext context) : ICommandHandler<CreateCategoryGroup, Result<CategoryGroupDto>>
{
	public async ValueTask<Result<CategoryGroupDto>> Handle(CreateCategoryGroup command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		if (!await context.Maps.AnyAsync(map => map.Id == command.MapId && map.GameId == command.GameId, cancellationToken))
			return CreateCategoryGroupErrors.MapNotFound;

		var siblings = await context.CategoryGroups.AsTracking().Where(group => group.MapId == command.MapId)
			.OrderBy(group => group.Order).ThenBy(group => group.CreatedAt).ThenBy(group => group.Id).ToListAsync(cancellationToken);

		var result = CategoryGroup.Create(command.MapId, command.Name, siblings.Count + 1, command.IconAssetId, command.Color);

		if (result.IsFailure)
			return result.Errors;

		var group = result.Value;

		var placement = CategoryPlacement.GetIndex(siblings.Select(item => item.Id).ToArray(), group.Id, command.BeforeId.GetValueOrDefault(null));

		if (placement.IsFailure)
			return placement.Errors;

		if (siblings.Any(other => other.NormalizedName == group.NormalizedName))
			return CreateCategoryGroupErrors.NameConflict;

		var icon = await context.FindOptionalImageAsync(command.IconAssetId, cancellationToken);

		if (icon.IsFailure)
			return icon.Errors;

		siblings.Insert(placement.Value, group);

		for (var index = 0; index < siblings.Count; index++)
			siblings[index].SetDisplayOrder(index + 1);

		context.CategoryGroups.Add(group);
		await context.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return group.ToDto(icon.Value?.ToDto());
	}
}
