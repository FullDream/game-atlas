using GameAtlas.Domain;

namespace GameAtlas.Api.Categories;

public sealed record CreateCategoryRequest
{
	public string? Name { get; init; }
	public Guid? IconAssetId { get; init; }
	public string? Color { get; init; }
	public Optional<int?> Order { get; init; }
}
