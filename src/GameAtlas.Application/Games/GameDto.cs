using GameAtlas.Application.Media;
using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games;

public sealed record GameDto(
	Guid Id,
	string Name,
	string Slug,
	ImageAssetDto Cover,
	ImageAssetDto? Banner,
	DateOnly? ReleaseDate,
	string? Developer,
	string? Description,
	int MarkersCount,
	int CategoriesCount,
	IReadOnlyList<GameMapDto> Maps,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	GameStatus Status);
