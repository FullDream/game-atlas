namespace GameAtlas.Application.CategoryGroups;

public sealed record CategoryGroupDto(
	Guid Id,
	Guid MapId,
	string Name,
	int Order,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt);
