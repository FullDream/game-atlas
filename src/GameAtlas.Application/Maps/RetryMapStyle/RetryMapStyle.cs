using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.RetryMapStyle;

public sealed record RetryMapStyle(Guid GameId, Guid MapId, Guid StyleId) : ICommand<Result<MapStyleDto>>;
