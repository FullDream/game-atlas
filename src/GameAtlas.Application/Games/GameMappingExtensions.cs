using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games;

internal static class GameMappingExtensions
{
	internal static GameDto ToDto(this Game game) =>
		new(game.Id, game.Name, game.Slug, game.Image, game.CreatedAt, game.UpdatedAt, game.Status);

	internal static IQueryable<GameDto> ToDto(this IQueryable<Game> games) =>
		games.Select(game => new GameDto(
			game.Id,
			game.Name,
			game.Slug,
			game.Image,
			game.CreatedAt,
			game.UpdatedAt,
			game.Status));
}
