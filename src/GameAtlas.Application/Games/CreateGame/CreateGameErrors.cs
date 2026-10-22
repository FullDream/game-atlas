using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Games.CreateGame;

public static class CreateGameErrors
{
	public static readonly Error SlugConflict =
		new("Game.Slug.Conflict", "A game with this slug already exists.", nameof(CreateGame.Slug), ErrorType.Conflict);
}
