using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Categories.Groups;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateCategoryGroupRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<string?> Color { get; init; }
	public Optional<Guid?> IconAssetId { get; init; }
	public Optional<Guid?> BeforeId { get; init; }
}
