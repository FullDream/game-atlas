using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Common.Sorting;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.ListPublicGames;

public sealed record ListPublicGames(
	int Offset = 0,
	int Limit = 20,
	string? Search = null,
	GameSortBy? SortBy = null,
	SortDirection? SortDirection = null) : IQuery<Result<PaginatedResult<PublicGameCardDto>>>;
