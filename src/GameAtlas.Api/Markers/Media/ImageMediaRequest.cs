using System.Text.Json.Serialization;

namespace GameAtlas.Api.Markers.Media;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ImageMediaRequest(string? Url, int? Width, int? Height, string? Alt = "") : MarkerMediaRequest;
