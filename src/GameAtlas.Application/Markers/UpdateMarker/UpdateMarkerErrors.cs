using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Markers.UpdateMarker;

public static class UpdateMarkerErrors
{
	public static readonly Error NotFound = new(
		"Marker.NotFound", "The marker was not found.",
		nameof(UpdateMarker.Id), ErrorType.NotFound);

	public static readonly Error CategoryNotFound = new(
		"Category.NotFound", "The category was not found in this game.",
		nameof(UpdateMarker.CategoryId), ErrorType.NotFound);

	public static readonly Error EmptyPatch = new(
		"Marker.Update.Empty", "At least one mutable field must be supplied.", null, ErrorType.Validation);
}
