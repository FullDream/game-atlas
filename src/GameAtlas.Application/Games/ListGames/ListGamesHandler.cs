using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Common.Sorting;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.ListGames;

public sealed class ListGamesHandler(IApplicationDbContext context)
	: IQueryHandler<ListGames, Result<PaginatedResult<GameDto>>>
{
	public async ValueTask<Result<PaginatedResult<GameDto>>> Handle(
		ListGames request, CancellationToken cancellationToken)
	{
		var query = context.Games.AsNoTracking();
		var search = request.Search?.Trim();

		if (!string.IsNullOrEmpty(search))
		{
			var pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

			query = query.Where(game => EF.Functions.Like(game.Name, pattern, "\\"));
		}

		var total = await query.CountAsync(cancellationToken);
		var ordered = (request.SortBy, request.SortDirection ?? SortDirection.Asc) switch
		{
			(GameSortBy.Name, SortDirection.Asc) => query.OrderBy(game => game.Name),
			(GameSortBy.Name, SortDirection.Desc) => query.OrderByDescending(game => game.Name),
			(GameSortBy.CreatedAt, SortDirection.Asc) => query.OrderBy(game => game.CreatedAt),
			(GameSortBy.CreatedAt, SortDirection.Desc) => query.OrderByDescending(game => game.CreatedAt),
			(GameSortBy.UpdatedAt, SortDirection.Asc) => query.OrderBy(game => game.UpdatedAt),
			(GameSortBy.UpdatedAt, SortDirection.Desc) => query.OrderByDescending(game => game.UpdatedAt),
			_ => null
		};

		if (ordered is not null)
			query = ordered.ThenBy(game => game.Id);

		var games = await query.Skip(request.Offset).Take(request.Limit).ToListAsync(cancellationToken);

		return new PaginatedResult<GameDto>(games.Select(game => game.ToDto()).ToArray(),
			request.Limit, request.Offset, total);
	}
}
