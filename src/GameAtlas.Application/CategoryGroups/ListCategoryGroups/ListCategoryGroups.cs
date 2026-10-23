using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.CategoryGroups.ListCategoryGroups;

public sealed record ListCategoryGroups(Guid GameId) : IQuery<Result<IReadOnlyCollection<CategoryGroupDto>>>;
