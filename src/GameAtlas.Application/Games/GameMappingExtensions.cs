using GameAtlas.Application.Media;
using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games;

internal static class GameMappingExtensions
{
	internal static GameDto ToDto(this Game game, ImageAssetDto? image = null) =>
		new(game.Id, game.Name, game.Slug, image ?? game.Image.ToDto(), game.CreatedAt, game.UpdatedAt, game.Status);
}
