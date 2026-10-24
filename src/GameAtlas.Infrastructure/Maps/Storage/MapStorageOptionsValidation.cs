using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Maps;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Maps.Storage;

public sealed class MapStorageOptionsValidation : IValidateOptions<MapStorageOptions>, IValidateOptions<MapUploadLimits>
{
	public ValidateOptionsResult Validate(string? name, MapStorageOptions options) =>
		!string.IsNullOrWhiteSpace(options.StorageRoot) && options.LeaseSeconds >= 30
			? ValidateOptionsResult.Success
			: ValidateOptionsResult.Fail("Maps requires a nonempty storage root and a lease of at least 30 seconds.");

	public ValidateOptionsResult Validate(string? name, MapUploadLimits options) =>
		options.FileBytes > 0 && options.FileBytes <= long.MaxValue - options.EnvelopeBytes &&
		options.MaximumDimension is > 0 and <= MapGeometry.MaximumDimension &&
		options.MetadataBytes is > 0 and <= 65536 && options.EnvelopeBytes >= options.MetadataBytes
			? ValidateOptionsResult.Success
			: ValidateOptionsResult.Fail("Map upload limits must be positive, bounded, and internally consistent.");
}
