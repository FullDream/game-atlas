using GameAtlas.Domain;

namespace GameAtlas.Api.CategoryGroups;

public sealed record CreateCategoryGroupRequest
{
	public string? Name { get; init; }
	public Optional<int?> Order { get; init; }
}
