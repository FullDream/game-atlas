using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Media;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateMediaAssetRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<Guid?> FolderId { get; init; }
	public Optional<string?> Alt { get; init; }
	public Optional<Guid?> PreviewAssetId { get; init; }
}
