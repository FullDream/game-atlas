using GameAtlas.Api.Http;
using GameAtlas.Api.Maps.Events;
using GameAtlas.Api.Maps.Uploads;
using GameAtlas.Api.Results;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.CreateMap;
using GameAtlas.Application.Maps.DeleteMap;
using GameAtlas.Application.Maps.DeleteMapStyle;
using GameAtlas.Application.Maps.GetMap;
using GameAtlas.Application.Maps.ListMaps;
using GameAtlas.Application.Maps.RetryMapStyle;
using Mediator;

namespace GameAtlas.Api.Maps;

public static class Endpoints
{
	public static RouteGroupBuilder MapMaps(this RouteGroupBuilder group)
	{
		var maps = group.MapGroup("/games/{gameId}/maps").WithTags("Maps");

		maps.MapGet("", ListMapsAsync).WithName("ListMaps").WithSummary("List maps");

		maps.MapGet("/{mapId}", GetMapAsync).WithName("GetMap").WithSummary("Get map");
		maps.MapGet("/{mapId}/events", StreamMapProgressAsync).WithName("StreamMapProgress")
			.WithSummary("Stream map progress").Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
			.ProducesProblem(404).ProducesProblem(500);

		maps.MapPost("", CreateMapAsync).WithName("CreateMap").WithSummary("Create map")
			.Accepts<CreateMapRequest>("application/json")
			.Produces<MapDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409)
			.ProducesProblem(413).ProducesProblem(415).ProducesProblem(422).ProducesProblem(500);

		maps.MapPost("/{mapId}/styles", AddMapStyleAsync).WithName("AddMapStyle").WithSummary("Add map style")
			.Accepts<MapUploadRequest>("multipart/form-data")
			.Produces<MapDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409)
			.ProducesProblem(413).ProducesProblem(415).ProducesProblem(422).ProducesProblem(500);

		maps.MapPost("/{mapId}/styles/{styleId}/retry", RetryMapStyleAsync)
			.WithName("RetryMapStyle").WithSummary("Retry map style");

		maps.MapPatch("/{mapId}/styles/{styleId}", UpdateMapStyleAsync)
			.WithName("UpdateMapStyle").WithSummary("Update map style")
			.WithMergePatch<UpdateMapStyleRequest>();

		maps.MapDelete("/{mapId}/styles/{styleId}", DeleteMapStyleAsync)
			.WithName("DeleteMapStyle").WithSummary("Delete map style");

		maps.MapDelete("/{mapId}", DeleteMapAsync).WithName("DeleteMap").WithSummary("Delete map");

		return maps;
	}

	private static async Task<OutcomeResult<IReadOnlyCollection<MapDto>>> ListMapsAsync(Guid gameId,
		IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new ListMaps(gameId), cancellationToken);

	private static async Task<OutcomeResult<MapDto>> GetMapAsync(Guid gameId, Guid mapId, IMediator mediator,
		CancellationToken cancellationToken) => await mediator.Send(new GetMap(gameId, mapId), cancellationToken);

	private static Task<IResult> StreamMapProgressAsync(Guid gameId, Guid mapId, HttpContext context,
		MapEventStream events,
		CancellationToken cancellationToken) => events.OpenAsync(gameId, mapId, context, cancellationToken);

	private static async Task<OutcomeResult<MapDto>> CreateMapAsync(Guid gameId, CreateMapRequest request,
		IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId), cancellationToken);

	private static Task<IResult> AddMapStyleAsync(Guid gameId, Guid mapId, HttpRequest request,
		MapUploadWorkflow uploads,
		CancellationToken cancellationToken) => uploads.SingleAsync(gameId, mapId, request, cancellationToken);

	private static async Task<OutcomeResult<MapStyleDto>> RetryMapStyleAsync(Guid gameId, Guid mapId, Guid styleId,
		IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new RetryMapStyle(gameId, mapId, styleId), cancellationToken);

	private static async Task<OutcomeResult<MapStyleDto>> UpdateMapStyleAsync(Guid gameId, Guid mapId, Guid styleId,
		UpdateMapStyleRequest request, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId, mapId, styleId), cancellationToken);

	private static async Task<OutcomeResult> DeleteMapStyleAsync(Guid gameId, Guid mapId, Guid styleId,
		IMediator mediator,
		CancellationToken cancellationToken) =>
		await mediator.Send(new DeleteMapStyle(gameId, mapId, styleId), cancellationToken);

	private static async Task<OutcomeResult> DeleteMapAsync(Guid gameId, Guid mapId, IMediator mediator,
		CancellationToken cancellationToken) => await mediator.Send(new DeleteMap(gameId, mapId), cancellationToken);
}
