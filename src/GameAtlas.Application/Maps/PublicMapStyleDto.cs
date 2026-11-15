namespace GameAtlas.Application.Maps;

public sealed record PublicMapStyleDto(Guid Id, string Name, string BackgroundColor,
	MapTileGeometryDto TileGeometry, string TilesUrlTemplate);
