using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.PublishMap;

public sealed record PublishMap(Guid GameId, Guid MapId) : ICommand<Result<MapDto>>;
