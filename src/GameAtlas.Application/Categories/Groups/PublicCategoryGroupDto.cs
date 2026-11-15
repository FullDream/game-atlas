using GameAtlas.Application.Media;

namespace GameAtlas.Application.Categories.Groups;

public sealed record PublicCategoryGroupDto(Guid Id, string Name, ImageAssetDto? Icon, string Color,
	int MarkersCount, IReadOnlyCollection<PublicCategoryDto> Categories);
