using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Markers.ListMarkers;

public sealed record ListMarkers(Guid GameId, Guid MapId) : IQuery<Result<IReadOnlyCollection<MarkerDto>>>;
