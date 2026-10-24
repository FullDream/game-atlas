using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.DeleteMap;

public sealed record DeleteMap(Guid GameId, Guid MapId) : ICommand<Result>;
