using GameAtlas.Application.Media;
using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games;

public sealed record GameDto(
	Guid Id,
	string Name,
	string Slug,
	ImageAssetDto Image,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	GameStatus Status);
