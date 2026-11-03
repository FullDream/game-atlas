using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.Groups.DeleteCategoryGroup;

public sealed record DeleteCategoryGroup(Guid GameId, Guid MapId, Guid Id) : ICommand<Result>;
