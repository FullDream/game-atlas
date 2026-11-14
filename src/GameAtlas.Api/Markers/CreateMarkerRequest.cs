using System.ComponentModel.DataAnnotations;
using GameAtlas.Domain;

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

	public Optional<PublicationStatus?> Status { get; init; }
}
