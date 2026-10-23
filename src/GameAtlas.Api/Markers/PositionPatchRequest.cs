using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Markers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PositionPatchRequest
{
	public Optional<double?> X { get; init; }
	public Optional<double?> Y { get; init; }
}
