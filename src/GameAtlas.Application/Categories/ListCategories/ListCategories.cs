using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.ListCategories;

public sealed record ListCategories(Guid GameId, Guid GroupId) : IQuery<Result<IReadOnlyCollection<CategoryDto>>>;
