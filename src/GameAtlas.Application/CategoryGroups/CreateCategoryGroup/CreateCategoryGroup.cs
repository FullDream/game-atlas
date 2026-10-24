using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.CategoryGroups.CreateCategoryGroup;

public sealed record CreateCategoryGroup(Guid GameId, Guid MapId, string? Name, Optional<int?> Order = default)
	: ICommand<Result<CategoryGroupDto>>;
