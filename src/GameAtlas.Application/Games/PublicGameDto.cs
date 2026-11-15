using GameAtlas.Application.Media;

namespace GameAtlas.Application.Games;

public sealed record PublicGameDto(
	Guid Id,
	string Name,
	string Slug,
	ImageAssetDto Cover,
	ImageAssetDto? Banner,
	DateOnly? ReleaseDate,
	string? Developer,
	string? Description,
	int MapsCount,
	int MarkersCount,
	int CategoriesCount,
	IReadOnlyList<PublicGameMapDto> Maps);
