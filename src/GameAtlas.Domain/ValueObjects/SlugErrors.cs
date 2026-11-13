using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.ValueObjects;

public static class SlugErrors
{
	public static readonly Error Required = new("Slug.Required", "Slug is required.", "Slug", ErrorType.Validation);
	public static readonly Error TooLong = new("Slug.Length", "Slug must contain at most 200 characters.", "Slug", ErrorType.Validation);
	public static readonly Error InvalidFormat = new("Slug.Format",
		"Slug must contain lowercase ASCII letters or digits separated by single hyphens.", "Slug", ErrorType.Validation);
}
