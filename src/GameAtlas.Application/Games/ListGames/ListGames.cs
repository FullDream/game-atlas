using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Common.Sorting;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.ListGames;

public sealed record ListGames(
	int Offset = 0,
	int Limit = 20,
	string? Search = null,
	GameSortBy? SortBy = null,
	SortDirection? SortDirection = null) : IQuery<Result<PaginatedResult<GameDto>>>;
