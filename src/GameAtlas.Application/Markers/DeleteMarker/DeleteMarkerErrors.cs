using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Markers.DeleteMarker;

public static class DeleteMarkerErrors
{
	public static readonly Error NotFound = new(
		"Marker.NotFound", "The marker was not found.",
		nameof(DeleteMarker.Id), ErrorType.NotFound);
}
