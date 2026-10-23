using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.CategoryGroups;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateCategoryGroupRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<int?> Order { get; init; }
}
