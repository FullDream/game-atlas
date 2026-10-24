using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Maps.Uploads;

public static class MapUploadErrors
{
	public static readonly Error ByteLimit = new(
		"Map.Upload.ByteLimit", "The upload exceeds the configured file byte limit.",
		"File", ErrorType.Validation);

	public static Error InvalidDimensions(int maximum) => new(
		"Map.Upload.Dimensions.Invalid", $"Width and height must be between 1 and {maximum}.",
		"File", ErrorType.Validation);
}
