using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers;

public static class MarkerErrors
{
	public static readonly Error InvalidName = new(
		"Marker.Name.Invalid", "Name must contain 1 through 200 characters after trimming.",
		nameof(Marker.Name), ErrorType.Validation);

	public static readonly Error InvalidDescription = new(
		"Marker.Description.Invalid", "Description must not exceed 5000 characters after trimming.",
		nameof(Marker.Description), ErrorType.Validation);

	public static readonly Error PositionRequired = new(
		"Marker.Position.Required", "Position must not be null.", nameof(Marker.Position), ErrorType.Validation);

	public static readonly Error CategoryRequired = new(
		"Marker.CategoryId.Required", "CategoryId must not be null.", nameof(Marker.CategoryId), ErrorType.Validation);

	public static readonly Error InvalidMedia = new(
		"Marker.Media.Invalid", "Media must be a non-null list of distinct attachments owned by this marker.",
		nameof(Marker.Media), ErrorType.Validation);
}
