using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Games.GetPublicGame;

public static class GetPublicGameErrors
{
	public static readonly Error NotFound = new("Game.NotFound", "The game was not found.", "slug", ErrorType.NotFound);
}
