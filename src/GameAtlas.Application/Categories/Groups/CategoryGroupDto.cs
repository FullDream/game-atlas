using GameAtlas.Application.Media;
using GameAtlas.Domain;

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
	IReadOnlyCollection<CategoryDto> Categories,
	PublicationStatus Status);
