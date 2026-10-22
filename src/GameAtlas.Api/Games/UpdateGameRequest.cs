using System.ComponentModel;
using System.Text.Json.Serialization;
using GameAtlas.Domain;
using GameAtlas.Domain.Games;

namespace GameAtlas.Api.Games;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Supply at least one mutable field. Omitted fields are preserved; null clears only mapId. " +
	"Identity and timestamps are read-only. Both supported media types use JSON Merge Patch semantics.")]
public sealed record UpdateGameRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<string?> Slug { get; init; }
	public Optional<string?> Image { get; init; }
	public Optional<Guid?> MapId { get; init; }
	public Optional<GameStatus?> Status { get; init; }
}
