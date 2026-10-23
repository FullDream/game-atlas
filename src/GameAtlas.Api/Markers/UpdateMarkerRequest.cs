using System.Text.Json.Serialization;
using GameAtlas.Api.Markers.Media;
using GameAtlas.Domain;

namespace GameAtlas.Api.Markers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateMarkerRequest
{
	public Optional<Guid?> CategoryId { get; init; }
	public Optional<string?> Name { get; init; }
	public Optional<string?> Description { get; init; }
	public Optional<PositionPatchRequest?> Position { get; init; }
	public Optional<IReadOnlyCollection<MarkerMediaRequest?>?> Media { get; init; }
}
