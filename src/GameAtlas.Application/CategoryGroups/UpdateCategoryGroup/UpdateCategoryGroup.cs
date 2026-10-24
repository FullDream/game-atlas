using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;

public sealed record UpdateCategoryGroup(
	Guid GameId,
	Guid MapId, Guid Id, Optional<string?> Name = default, Optional<int?> Order = default)
	: ICommand<Result<CategoryGroupDto>>;
