using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games.CreateGame;

internal static class GameMappingExtensions
{
	internal static GameDto ToDto(this Game game) =>
		new(game.Id, game.Name, game.Slug, game.Image, game.MapId, game.CreatedAt, game.UpdatedAt, game.Status);
}
