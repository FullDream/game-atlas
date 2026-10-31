using System.ComponentModel;
using System.Text.Json.Serialization;
using GameAtlas.Domain;
using GameAtlas.Domain.Games;

namespace GameAtlas.Api.Games;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Supply at least one mutable field. Omitted fields are preserved; null is invalid for required fields. " +
	"Identity, mapId, and timestamps are read-only. Both supported media types use JSON Merge Patch semantics.")]
public sealed record UpdateGameRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<string?> Slug { get; init; }
	public Optional<Guid?> CoverId { get; init; }
	public Optional<Guid?> BannerId { get; init; }
	public Optional<DateOnly?> ReleaseDate { get; init; }
	public Optional<string?> Developer { get; init; }
	public Optional<string?> Description { get; init; }
	public Optional<GameStatus?> Status { get; init; }
}
