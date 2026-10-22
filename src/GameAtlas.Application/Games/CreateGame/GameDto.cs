using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Games.CreateGame;

public sealed record GameDto(
	Guid Id,
	string Name,
	string Slug,
	string Image,
	Guid? MapId,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	GameStatus Status);
