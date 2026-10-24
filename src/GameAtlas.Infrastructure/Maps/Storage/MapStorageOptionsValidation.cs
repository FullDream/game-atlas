using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Maps.Storage;

public sealed class MapStorageOptionsValidation : IValidateOptions<MapStorageOptions>
{
	public ValidateOptionsResult Validate(string? name, MapStorageOptions options) =>
		!string.IsNullOrWhiteSpace(options.StorageRoot)
			? ValidateOptionsResult.Success
			: ValidateOptionsResult.Fail("Maps requires a nonempty storage root.");
}
