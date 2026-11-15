using GameAtlas.Application.Categories;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Maps;

internal static class PublicMapMappingExtensions
{
	internal static PublicMapDetailsDto ToPublicDto(this Map map, IReadOnlyCollection<MapStyle> styles,
		IReadOnlyCollection<CategoryGroup> groups, IReadOnlyCollection<Category> categories,
		IReadOnlyCollection<Marker> markers, IReadOnlyDictionary<Guid, MediaAssetDto> assets)
	{
		var counts = markers.CountBy(marker => marker.CategoryId).ToDictionary();

		var children = categories.Select(category => new PublicCategoryDto(category.Id, category.CategoryGroupId,
			category.Name, category.IconAssetId is { } iconId ? (ImageAssetDto)assets[iconId] : null,
			category.Color, counts.GetValueOrDefault(category.Id))).ToLookup(category => category.CategoryGroupId);

		var tree = groups.Select(group => new PublicCategoryGroupDto(group.Id, group.Name,
			group.IconAssetId is { } iconId ? (ImageAssetDto)assets[iconId] : null, group.Color,
			children[group.Id].Sum(category => category.MarkersCount), children[group.Id].ToArray())).ToArray();

		var publicMarkers = markers.Select(marker => new PublicMarkerDto(marker.Id, marker.CategoryId,
			marker.Name, marker.Description, marker.Position.ToDto(),
			marker.Media.OrderBy(item => item.Order).Select(item => assets[item.AssetId]).ToArray())).ToArray();

		var publicStyles = styles.Select(style => style.ToPublicDto()).ToArray();
		var defaultId = publicStyles.Any(style => style.Id == map.DefaultStyleId) ? map.DefaultStyleId : null;

		return new PublicMapDetailsDto(map.Id, map.GameId, map.Name, map.Slug, map.Width, map.Height,
			map.Geometry?.Extent, map.Geometry?.TileOrigin, map.Geometry is null ? null : MapGeometry.TileSize,
			map.Geometry?.MinZoom, map.Geometry?.MaxZoom, map.Geometry?.Resolutions, defaultId, publicStyles, tree, publicMarkers);
	}

	private static PublicMapStyleDto ToPublicDto(this MapStyle style)
	{
		var geometry = MapGeometry.Create(style.Width, style.Height).Value!;

		return new PublicMapStyleDto(style.Id, style.Name, style.BackgroundColor,
			new MapTileGeometryDto(geometry.Extent, geometry.TileOrigin, MapGeometry.TileSize,
				geometry.MinZoom, geometry.MaxZoom, geometry.Resolutions),
			$"/map-tiles/{style.MapId:N}/{style.Id:N}/{{z}}/{{x}}/{{y}}.webp");
	}
}
