using GameAtlas.Api.Http;
using GameAtlas.Api.Results;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Markers.DeleteMarker;
using GameAtlas.Application.Markers.ListMarkers;
using Mediator;

namespace GameAtlas.Api.Markers;

public static class Endpoints
{
	public static RouteGroupBuilder MapMarkers(this RouteGroupBuilder group)
	{
		var markers = group.MapGroup("/games/{gameId}/maps/{mapId}/markers").WithTags("Markers");

		markers.MapGet("", ListMarkersAsync)
			.WithName("ListMarkers").WithSummary("List markers")
			.WithDescription("Returns the complete list ordered by createdAt and id, with ordered media. " +
				"No filtering or pagination is supported.");

		markers.MapPost("", CreateMarkerAsync)
			.WithName("CreateMarker").WithSummary("Create marker")
			.WithDescription("Creates a marker in a category of this game. Media are ordered references; " +
				"no files are uploaded or fetched. Duplicate names are allowed.");

		markers.MapPatch("/{id}", UpdateMarkerAsync)
			.WithName("UpdateMarker").WithSummary("Update marker")
			.WithDescription("Merges supplied position coordinates recursively and replaces supplied media arrays " +
				"as a whole. Omitted fields remain unchanged. Description and video preview may be cleared. " +
				"Category transfers must stay within this game; no-ops preserve updatedAt.")
			.WithMergePatch<UpdateMarkerRequest>();

		markers.MapDelete("/{id}", DeleteMarkerAsync)
			.WithName("DeleteMarker").WithSummary("Delete marker")
			.WithDescription("Permanently deletes the marker and its media rows, returning empty 204. " +
				"Referenced files are not deleted. Missing markers return 404.");

		return markers;
	}

	private static async Task<OutcomeResult<IReadOnlyCollection<MarkerDto>>> ListMarkersAsync(
		Guid gameId, Guid mapId, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new ListMarkers(gameId, mapId), cancellationToken);

	private static async Task<OutcomeResult<MarkerDto>> CreateMarkerAsync(
		Guid gameId, Guid mapId, CreateMarkerRequest request, IMediator mediator,
			CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId, mapId), cancellationToken);

	private static async Task<OutcomeResult<MarkerDto>> UpdateMarkerAsync(
		Guid gameId, Guid mapId, Guid id, UpdateMarkerRequest request, IMediator mediator,
			CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId, mapId, id), cancellationToken);

	private static async Task<OutcomeResult> DeleteMarkerAsync(
		Guid gameId, Guid mapId, Guid id, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new DeleteMarker(gameId, mapId, id), cancellationToken);
}
