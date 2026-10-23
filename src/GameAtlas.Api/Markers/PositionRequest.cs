using System.Text.Json.Serialization;

namespace GameAtlas.Api.Markers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PositionRequest(double? X, double? Y);
