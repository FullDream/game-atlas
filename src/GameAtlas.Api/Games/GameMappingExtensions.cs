using GameAtlas.Application.Games.CreateGame;
using GameAtlas.Application.Games.ListGames;

namespace GameAtlas.Api.Games;

internal static class GameMappingExtensions
{
	internal static CreateGame ToCommand(this CreateGameRequest request) =>
		new(request.Name, request.Slug, request.Image, request.MapId);

	internal static ListGames ToQuery(this ListGamesRequest request) =>
		new(request.Offset ?? 0, request.Limit ?? 20, request.Search, request.SortBy, request.SortDirection);
}
