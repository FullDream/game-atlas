using GameAtlas.Application.Markers.Media;

namespace GameAtlas.Application.Markers;

public sealed record MarkerDto(Guid Id, Guid CategoryId, string Name, string? Description, PositionDto Position,
	IReadOnlyCollection<MarkerMediaDto> Media, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
