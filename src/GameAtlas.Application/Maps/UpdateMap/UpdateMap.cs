using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.UpdateMap;

public sealed record UpdateMap(Guid GameId, Guid MapId, Optional<string?> Name = default) : ICommand<Result<MapDto>>;
