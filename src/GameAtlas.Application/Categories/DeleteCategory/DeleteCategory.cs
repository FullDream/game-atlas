using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.DeleteCategory;

public sealed record DeleteCategory(Guid GameId, Guid GroupId, Guid Id) : ICommand<Result>;
