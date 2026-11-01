using System.ComponentModel;
using System.Text.Json.Serialization;
using GameAtlas.Domain;

namespace GameAtlas.Api.Maps;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Supply name, defaultStyleId or the complete styleOrder. Null is invalid; geometry is immutable.")]
public sealed record UpdateMapRequest
{
	public Optional<string?> Name { get; init; }
	public Optional<Guid?> DefaultStyleId { get; init; }
	public Optional<IReadOnlyList<Guid?>?> StyleOrder { get; init; }
}
