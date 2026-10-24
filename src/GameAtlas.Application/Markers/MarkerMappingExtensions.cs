using GameAtlas.Application.Media;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers;

internal static class MarkerMappingExtensions
{
	internal static MarkerDto ToDto(this Marker marker, IReadOnlyDictionary<Guid, MediaAssetDto> assets) => new(
		marker.Id,
		marker.CategoryId,
		marker.Name,
		marker.Description,
		marker.Position.ToDto(),
		marker.Media.OrderBy(item => item.Order).Select(item => assets[item.AssetId]).ToArray(),
		marker.CreatedAt,
		marker.UpdatedAt);

	internal static PositionDto ToDto(this Position position) => new(position.X, position.Y);
}
