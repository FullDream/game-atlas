using GameAtlas.Application.Media;
using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games;

internal static class GameMappingExtensions
{
	internal static GameDto ToDto(this Game game, ImageAssetDto cover, ImageAssetDto? banner,
		int markersCount, int categoriesCount, IReadOnlyList<GameMapDto> maps) =>
		new(game.Id, game.Name, game.Slug, cover, banner, game.ReleaseDate, game.Developer,
			game.Description, markersCount, categoriesCount, maps, game.CreatedAt, game.UpdatedAt, game.Status);
}
