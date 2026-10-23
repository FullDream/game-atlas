using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;

public sealed class UpdateCategoryGroupHandler(IApplicationDbContext context)
	: ICommandHandler<UpdateCategoryGroup, Result<CategoryGroupDto>>
{
	public async ValueTask<Result<CategoryGroupDto>> Handle(
		UpdateCategoryGroup command, CancellationToken cancellationToken)
	{
		await using var transaction = await context.BeginTransactionAsync(cancellationToken);
		var group = await context.CategoryGroups.AsTracking().SingleOrDefaultAsync(
			group => group.GameId == command.GameId && group.Id == command.Id, cancellationToken);

		if (group is null)
			return UpdateCategoryGroupErrors.NotFound;

		var nextName = command.Name.GetValueOrDefault(group.Name);

		if (command.Name.IsSpecified && !string.IsNullOrWhiteSpace(nextName) &&
			CategoryGroup.NormalizeName(nextName) != group.NormalizedName)
		{
			var normalizedName = CategoryGroup.NormalizeName(nextName);
			var nameExists = await context.CategoryGroups.AnyAsync(other => other.GameId == command.GameId &&
				other.Id != command.Id && other.NormalizedName == normalizedName, cancellationToken);

			if (nameExists)
				return UpdateCategoryGroupErrors.NameConflict;
		}

		var result = group.Update(command.Name, command.Order);

		if (result.IsFailure)
			return result.Errors;

		if (result.Value)
			await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return group.ToDto();
	}
}
