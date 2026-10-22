using GameAtlas.Application.Common.Pagination;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.ListGames;

public sealed record ListGames(
	int Offset = 0,
	int Limit = 20,
	string? Search = null,
	string? SortBy = null,
	string? SortDirection = null) : IQuery<Result<PaginatedResult<GameDto>>>;
