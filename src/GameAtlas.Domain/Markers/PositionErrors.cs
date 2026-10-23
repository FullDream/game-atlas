using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers;

public static class PositionErrors
{
	public static readonly Error InvalidCoordinates = new(
		"Marker.Position.Invalid", "Position coordinates must be finite numbers.", "Position", ErrorType.Validation);
}
