using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.ListCategories;

public sealed record ListCategories(Guid GameId, Guid MapId,
	Guid GroupId) : IQuery<Result<IReadOnlyCollection<CategoryDto>>>;
