using GameAtlas.Api.Results;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.GetPublicMap;
using Mediator;

namespace GameAtlas.Api.Maps;

public static class PublicEndpoints
{
	public static RouteGroupBuilder MapPublicMaps(this RouteGroupBuilder group)
	{
		var maps = group.MapGroup("/public/games/{gameSlug}/maps").WithTags("Public Maps");

		maps.MapGet("/{mapSlug}", GetAsync).WithName("GetPublicMap")
			.ProducesProblem(StatusCodes.Status404NotFound)
			.WithSummary("Get a published map with categories and markers");

		return maps;
	}

	private static async Task<OutcomeResult<PublicMapDetailsDto>> GetAsync(string gameSlug, string mapSlug,
		IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new GetPublicMap(gameSlug, mapSlug, ContentVisibility.PublishedOnly), cancellationToken);
}
