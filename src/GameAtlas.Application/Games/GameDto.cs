using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games;

public sealed record GameDto(
	Guid Id,
	string Name,
	string Slug,
	string Image,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	GameStatus Status);
