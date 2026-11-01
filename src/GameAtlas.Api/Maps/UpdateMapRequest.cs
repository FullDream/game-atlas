using System.ComponentModel;
using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Maps;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Supply name to rename the map. Null is invalid; dimensions and geometry are immutable.")]
public sealed record UpdateMapRequest
{
	public Optional<string?> Name { get; init; }
}
