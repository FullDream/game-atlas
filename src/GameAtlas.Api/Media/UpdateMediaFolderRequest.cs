using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Media;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateMediaFolderRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<Guid?> ParentId { get; init; }
}
