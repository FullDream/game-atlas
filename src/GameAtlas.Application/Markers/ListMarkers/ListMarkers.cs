using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Markers.ListMarkers;

public sealed record ListMarkers(Guid GameId) : IQuery<Result<IReadOnlyCollection<MarkerDto>>>;
