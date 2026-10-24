using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps;

public static class MapMappingExtensions
{
	public static MapDto ToDto(this Map map) => new(
		map.Id,
		map.GameId,
		map.Name,
		map.Width,
		map.Height,
		map.Geometry.Extent,
		map.Geometry.TileOrigin,
		MapGeometry.TileSize,
		map.Geometry.MinZoom,
		map.Geometry.MaxZoom,
		map.Geometry.Resolutions,
		map.DefaultStyleId,
		map.Styles.OrderBy(style => style.CreationSequence)
			.Select(style => style.ToDto()).ToArray(),
		map.CreatedAt,
		map.UpdatedAt);

	public static MapStyleDto ToDto(this MapStyle style)
	{
		var error = style.ErrorCode is { } code && style.ErrorMessage is { } message
			? new MapProcessingErrorDto(code, message)
			: null;
		var template = style.Status == MapProcessingStatus.Ready
			? $"/map-tiles/{style.MapId:N}/{style.Id:N}/{{z}}/{{x}}/{{y}}.webp"
			: null;

		return new MapStyleDto(
			style.Id,
			style.Name,
			style.BackgroundColor,
			style.Status,
			style.Progress,
			error,
			template,
			style.CreatedAt,
			style.UpdatedAt);
	}
}
