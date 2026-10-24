using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.GetMap;

public sealed record GetMap(Guid GameId, Guid MapId) : IQuery<Result<MapDto>>;
