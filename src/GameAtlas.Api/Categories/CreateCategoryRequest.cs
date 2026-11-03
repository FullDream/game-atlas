using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Categories;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateCategoryRequest
{
	public string? Name { get; init; }
	public Guid? IconAssetId { get; init; }
	public string? Color { get; init; }
	public Optional<Guid?> BeforeId { get; init; }
}
