using GameAtlas.Application.Media;

namespace GameAtlas.Application.Categories;

public sealed record PublicCategoryDto(Guid Id, Guid CategoryGroupId, string Name, ImageAssetDto? Icon,
	string? Color, int MarkersCount);
