using GameAtlas.Application.Media;

namespace GameAtlas.Application.Categories.Groups;

public sealed record CategoryGroupDto(
	Guid Id,
	Guid MapId,
	string Name,
	string Color,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	ImageAssetDto? Icon,
	int MarkersCount,
	IReadOnlyCollection<CategoryDto> Categories);
