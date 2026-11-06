using GameAtlas.Application.Media;

namespace GameAtlas.Application.Categories;

public sealed record CategoryDto(
	Guid Id,
	Guid CategoryGroupId,
	string Name,
	ImageAssetDto? Icon,
	string? Color,
	int MarkersCount,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt);
