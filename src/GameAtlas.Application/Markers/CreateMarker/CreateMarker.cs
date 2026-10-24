using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Markers.CreateMarker;

public sealed record CreateMarker(Guid GameId, Guid MapId, Guid? CategoryId, string? Name, PositionInput? Position,
	string? Description, IReadOnlyCollection<Guid?>? Media) : ICommand<Result<MarkerDto>>;
