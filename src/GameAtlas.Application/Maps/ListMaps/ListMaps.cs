using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.ListMaps;

public sealed record ListMaps(Guid GameId) : IQuery<Result<IReadOnlyCollection<MapDto>>>;
