using GameAtlas.Application.Media;

namespace GameAtlas.Application.Games;

public sealed record PublicGameCardDto(Guid Id, string Name, string Slug, ImageAssetDto Cover,
	ImageAssetDto? Banner, int MapsCount, int MarkersCount, int CategoriesCount);
