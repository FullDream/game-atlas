using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.CategoryGroups.DeleteCategoryGroup;

public sealed record DeleteCategoryGroup(Guid GameId, Guid MapId, Guid Id) : ICommand<Result>;
