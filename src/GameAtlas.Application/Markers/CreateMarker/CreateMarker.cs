using GameAtlas.Application.Markers.Media;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Markers.CreateMarker;

public sealed record CreateMarker(Guid GameId, Guid? CategoryId, string? Name, PositionInput? Position,
	string? Description, IReadOnlyCollection<MarkerMediaInput?>? Media) : ICommand<Result<MarkerDto>>;
