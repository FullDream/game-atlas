using GameAtlas.Application.Media.Storage;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Media.Storage;

public sealed class MediaOptionsValidation : IValidateOptions<MediaLimits>, IValidateOptions<MediaStorageOptions>
{
	public ValidateOptionsResult Validate(string? name, MediaLimits options) =>
		options.MaximumBytes is > 0 and <= long.MaxValue - 65536 && options.MaximumImageSide is > 0 and <= 16384 &&
		options.PlaceholderSide is > 0 and <= 256 && options.MaximumPlaceholderBytes is >= 128 and <= 65536 &&
		options.MaximumXmlDepth is > 0 and <= 256 && options.MaximumXmlCharacters is > 0 and <= 314572800 &&
		options.MaximumXmlElements > 0 &&
		options.MaximumXmlAttributes is > 0 and <= 256 && options.ProbeTimeoutSeconds is > 0 and <= 120 &&
		options.VideoInspectionBytes is >= 65536 and <= 314572800 &&
		options.ConcurrentVideoInspections is >= 1 and <= 16
			? ValidateOptionsResult.Success
			: ValidateOptionsResult.Fail("Media inspection limits must be positive and within supported bounds.");

	public ValidateOptionsResult Validate(string? name, MediaStorageOptions options) =>
		!string.IsNullOrWhiteSpace(options.StorageRoot)
			? ValidateOptionsResult.Success
			: ValidateOptionsResult.Fail("Media storage root is required.");
}
