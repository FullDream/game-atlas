using System.ComponentModel;
using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Maps;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Supply at least one mutable field. Omission preserves values; null is invalid. " +
	"Only name and viewer backgroundColor may be changed; no image processing is queued.")]
public sealed record UpdateMapStyleRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<string?> BackgroundColor { get; init; }
}
