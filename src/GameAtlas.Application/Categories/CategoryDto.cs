namespace GameAtlas.Application.Categories;

public sealed record CategoryDto(
	Guid Id,
	Guid CategoryGroupId,
	string Name,
	string Icon,
	string Color,
	int Order,
	int MarkersCount,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt);
