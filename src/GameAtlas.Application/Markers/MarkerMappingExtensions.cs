using GameAtlas.Application.Markers.Media;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers;

internal static class MarkerMappingExtensions
{
	internal static MarkerDto ToDto(this Marker marker) => new(
		marker.Id,
		marker.CategoryId,
		marker.Name,
		marker.Description,
		marker.Position.ToDto(),
		marker.Media.OrderBy(item => item.Order).Select(item => item.ToDto()).ToArray(),
		marker.CreatedAt,
		marker.UpdatedAt);

	internal static PositionDto ToDto(this Position position) => new(position.X, position.Y);
}
