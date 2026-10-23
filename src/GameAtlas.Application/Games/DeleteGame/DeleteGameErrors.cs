using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Games.DeleteGame;

public static class DeleteGameErrors
{
	public static readonly Error NotFound = new(
		"Game.NotFound", "The game was not found.", nameof(DeleteGame.Id), ErrorType.NotFound);
}
