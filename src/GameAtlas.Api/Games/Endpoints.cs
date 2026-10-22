using GameAtlas.Api.Results;
using GameAtlas.Application.Games;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Games.CreateGame;
using Mediator;

namespace GameAtlas.Api.Games;

public static class Endpoints
{
	public static RouteGroupBuilder MapGames(this RouteGroupBuilder group)
	{
		var games = group.MapGroup("/games").WithTags("Games");

		games.MapGet("", ListGamesAsync)
			.WithName("ListGames")
			.WithSummary("List games")
			.WithDescription("Lists all game statuses with offset/limit pagination and optional name search. " +
							 "Search ignores ASCII case. Without sortBy, ordering is unspecified.")
			.ProducesProblem(StatusCodes.Status500InternalServerError);

		games.MapPost("", CreateGameAsync)
			.WithName("CreateGame")
			.WithSummary("Create game")
			.WithDescription("Creates a draft with a client-supplied unique slug. " +
							 "Trims required strings. Returns the complete stored game.")
			.ProducesProblem(StatusCodes.Status500InternalServerError);

		return games;
	}

	private static async Task<OutcomeResult<GameDto>> CreateGameAsync(
		CreateGameRequest request, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(), cancellationToken);

	private static async Task<OutcomeResult<PaginatedResult<GameDto>>> ListGamesAsync(
		[AsParameters] ListGamesRequest request, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToQuery(), cancellationToken);
}

public sealed record CreateGameRequest(string? Name, string? Slug, string? Image, Guid? MapId = null);
