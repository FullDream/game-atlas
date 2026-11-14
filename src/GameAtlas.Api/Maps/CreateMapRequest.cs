using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Maps;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateMapRequest(string? Name, string? Slug)
{
	public Optional<PublicationStatus?> Status { get; init; }
}
