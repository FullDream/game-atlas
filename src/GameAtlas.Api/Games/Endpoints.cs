using GameAtlas.Api.Http;
using GameAtlas.Api.Results;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Games;
using GameAtlas.Application.Games.CreateGame;
using GameAtlas.Application.Games.DeleteGame;
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
							 "Search ignores ASCII case. Without sortBy, ordering is unspecified.");

		games.MapPost("", CreateGameAsync)
			.WithName("CreateGame")
			.WithSummary("Create game")
			.WithDescription("Creates a draft with a client-supplied unique slug. " +
							 "Trims required strings. Returns the complete stored game.");

		games.MapPatch("/{id}", UpdateGameAsync)
			.WithName("UpdateGame")
			.WithSummary("Update game")
			.WithDescription("Updates supplied fields using JSON Merge Patch semantics " +
				"with either supported media type. " +
				"Supply at least one mutable field. Omitted fields are preserved; null is invalid. " +
				"Identity and timestamps are read-only. Status supports draft and published. " +
				"Unchanged values preserve updatedAt. Returns the complete stored game.")
			.WithMergePatch<UpdateGameRequest>();

		games.MapDelete("/{id}", DeleteGameAsync)
			.WithName("DeleteGame")
			.WithSummary("Delete game")
			.WithDescription("Permanently deletes a draft or published game. Returns 204 without a body. " +
				"Missing games, including repeated deletion, return 404. The deleted slug becomes reusable.");

		return games;
	}

	private static async Task<OutcomeResult<GameDto>> CreateGameAsync(
		CreateGameRequest request, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(), cancellationToken);

	private static async Task<OutcomeResult<PaginatedResult<GameDto>>> ListGamesAsync(
		[AsParameters] ListGamesRequest request, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToQuery(), cancellationToken);

	private static async Task<OutcomeResult<GameDto>> UpdateGameAsync(
		Guid id, UpdateGameRequest request, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(id), cancellationToken);

	private static async Task<OutcomeResult> DeleteGameAsync(
		Guid id, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new DeleteGame(id), cancellationToken);
}
