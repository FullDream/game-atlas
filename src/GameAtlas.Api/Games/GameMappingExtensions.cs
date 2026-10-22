using GameAtlas.Application.Games.CreateGame;

namespace GameAtlas.Api.Games;

internal static class GameMappingExtensions
{
	internal static CreateGame ToCommand(this CreateGameRequest request) =>
		new(request.Name, request.Slug, request.Image, request.MapId);
}
