using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Markers.DeleteMarker;

public sealed record DeleteMarker(Guid GameId, Guid Id) : ICommand<Result>;
