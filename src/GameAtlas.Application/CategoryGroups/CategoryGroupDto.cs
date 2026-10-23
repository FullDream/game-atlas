namespace GameAtlas.Application.CategoryGroups;

public sealed record CategoryGroupDto(
	Guid Id,
	Guid GameId,
	string Name,
	int Order,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt);
