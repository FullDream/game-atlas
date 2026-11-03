using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.Groups.ListCategoryGroups;

public sealed record ListCategoryGroups(Guid GameId,
	Guid MapId) : IQuery<Result<IReadOnlyCollection<CategoryGroupDto>>>;
