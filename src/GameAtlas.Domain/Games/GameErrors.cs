using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Games;

public static class GameErrors
{
	public static readonly Error StatusRequired =
		new("Game.Status.Required", "Status is required.", nameof(Game.Status), ErrorType.Validation);

	public static readonly Error InvalidStatus =
		new("Game.Status.Invalid", "Status must be draft or published.", nameof(Game.Status), ErrorType.Validation);

	public static readonly Error NameRequired =
		new("Game.Name.Required", "Name is required.", nameof(Game.Name), ErrorType.Validation);

	public static readonly Error SlugRequired =
		new("Game.Slug.Required", "Slug is required.", nameof(Game.Slug), ErrorType.Validation);

	public static readonly Error ImageRequired =
		new("Game.Image.Required", "Image is required.", nameof(Game.Image), ErrorType.Validation);

	public static readonly Error InvalidSlug = new(
		"Game.Slug.Format",
		"Slug must contain lowercase ASCII letters or digits separated by single hyphens.",
		nameof(Game.Slug),
		ErrorType.Validation);
}
