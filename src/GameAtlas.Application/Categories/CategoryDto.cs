using GameAtlas.Application.Media;
using GameAtlas.Domain;

namespace GameAtlas.Application.Categories;

public sealed record CategoryDto(
	Guid Id,
	Guid CategoryGroupId,
	string Name,
	ImageAssetDto? Icon,
	string? Color,
	int MarkersCount,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	PublicationStatus Status);
