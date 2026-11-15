using GameAtlas.Application.Media;

namespace GameAtlas.Application.Markers;

public sealed record PublicMarkerDto(Guid Id, Guid CategoryId, string Name, string? Description,
	PositionDto Position, IReadOnlyCollection<MediaAssetDto> Media);
