using System.ComponentModel.DataAnnotations;

namespace GameAtlas.Api.Markers;

public sealed record CreateMarkerRequest
{
	[Required]
	public Guid? CategoryId { get; init; }

	[Required]
	public string? Name { get; init; }

	public string? Description { get; init; }

	[Required]
	public PositionRequest? Position { get; init; }

	public IReadOnlyCollection<Guid?>? Media { get; init; } = [];
}
