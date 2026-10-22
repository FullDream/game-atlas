using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Games.UpdateGame;

public static class UpdateGameErrors
{
	public static readonly Error EmptyPatch = new(
		"Game.Update.Empty", "At least one mutable field must be supplied.", null, ErrorType.Validation);

	public static readonly Error NotFound = new(
		"Game.NotFound", "The game was not found.", nameof(UpdateGame.Id), ErrorType.NotFound);

	public static readonly Error SlugConflict = new(
		"Game.Slug.Conflict", "A game with this slug already exists.", nameof(UpdateGame.Slug), ErrorType.Conflict);
}
