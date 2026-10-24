using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Categories;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateCategoryRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<Guid?> IconAssetId { get; init; }
	public Optional<string?> Color { get; init; }
	public Optional<int?> Order { get; init; }
	public Optional<Guid?> CategoryGroupId { get; init; }
}
