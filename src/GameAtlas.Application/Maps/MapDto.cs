namespace GameAtlas.Application.Maps;

public sealed record MapDto(
	Guid Id,
	Guid GameId,
	string Name,
	int Width,
	int Height,
	IReadOnlyCollection<double> Extent,
	IReadOnlyCollection<double> TileOrigin,
	int TileSize,
	int MinZoom,
	int MaxZoom,
	IReadOnlyCollection<double> Resolutions,
	Guid? DefaultStyleId,
	IReadOnlyCollection<MapStyleDto> Styles,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt);
