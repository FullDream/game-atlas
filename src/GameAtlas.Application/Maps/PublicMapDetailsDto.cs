using GameAtlas.Application.Categories.Groups;
using GameAtlas.Application.Markers;

namespace GameAtlas.Application.Maps;

public sealed record PublicMapDetailsDto(Guid Id, Guid GameId, string Name, string Slug,
	int? Width, int? Height, IReadOnlyCollection<double>? Extent, IReadOnlyCollection<double>? TileOrigin,
	int? TileSize, int? MinZoom, int? MaxZoom, IReadOnlyCollection<double>? Resolutions, Guid? DefaultStyleId,
	IReadOnlyCollection<PublicMapStyleDto> Styles, IReadOnlyCollection<PublicCategoryGroupDto> CategoryGroups,
	IReadOnlyCollection<PublicMarkerDto> Markers);
