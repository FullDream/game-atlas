using System.Text.Json.Serialization;

namespace GameAtlas.Api.Markers.Media;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record VideoMediaRequest(string? Url, string? Preview = null) : MarkerMediaRequest;
