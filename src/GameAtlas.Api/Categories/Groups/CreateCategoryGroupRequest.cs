using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Categories.Groups;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateCategoryGroupRequest
{
	public string? Name { get; init; }
	public string? Color { get; init; } = "#F59E0B";
	public Guid? IconAssetId { get; init; }
	public Optional<Guid?> BeforeId { get; init; }
}
