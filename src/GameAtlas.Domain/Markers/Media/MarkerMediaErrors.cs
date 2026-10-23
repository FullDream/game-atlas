using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers.Media;

public static class MarkerMediaErrors
{
	public static readonly Error InvalidUrl = new(
		"Marker.Media.Url.Invalid", "Url must contain 1 through 2048 characters after trimming.",
		"Url", ErrorType.Validation);

	public static readonly Error InvalidDimensions = new(
		"Marker.Media.Dimensions.Invalid", "Image dimensions must be positive integers.", null, ErrorType.Validation);

	public static readonly Error AltRequired = new(
		"Marker.Media.Alt.Required", "Alt must not be null.", "Alt", ErrorType.Validation);

	public static readonly Error InvalidVideoId = new(
		"Marker.Media.VideoId.Invalid", "VideoId must contain 11 ASCII letters, digits, hyphens, or underscores.",
		"VideoId", ErrorType.Validation);

	public static readonly Error InvalidPreview = new(
		"Marker.Media.Preview.Invalid", "Preview must not exceed 2048 characters after trimming.",
		"Preview", ErrorType.Validation);
}
