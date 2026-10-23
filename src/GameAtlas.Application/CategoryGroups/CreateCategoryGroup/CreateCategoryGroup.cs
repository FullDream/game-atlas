using GameAtlas.Domain.Result;
using GameAtlas.Domain;
using Mediator;

namespace GameAtlas.Application.CategoryGroups.CreateCategoryGroup;

public sealed record CreateCategoryGroup(Guid GameId, string? Name, Optional<int?> Order = default)
	: ICommand<Result<CategoryGroupDto>>;
