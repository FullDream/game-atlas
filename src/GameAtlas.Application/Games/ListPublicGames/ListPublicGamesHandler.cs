using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Common.Sorting;
using GameAtlas.Application.Media;
using GameAtlas.Domain;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.ListPublicGames;

public sealed class ListPublicGamesHandler(IApplicationDbContext context)
	: IQueryHandler<ListPublicGames, Result<PaginatedResult<PublicGameCardDto>>>
{
	public async ValueTask<Result<PaginatedResult<PublicGameCardDto>>> Handle(
		ListPublicGames request, CancellationToken cancellationToken)
	{
		var query = context.Games.AsNoTracking().Where(game => game.Status == PublicationStatus.Published);
		var search = request.Search?.Trim();

		if (!string.IsNullOrEmpty(search))
		{
			var pattern = "%" + search.ToLower().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

			query = query.Where(game => EF.Functions.Like(game.Name.ToLower(), pattern, "\\"));
		}

		var total = await query.CountAsync(cancellationToken);

		var ordered = (request.SortBy, request.SortDirection ?? SortDirection.Asc) switch
		{
			(GameSortBy.Name, SortDirection.Asc) => query.OrderBy(game => game.Name),
			(GameSortBy.Name, SortDirection.Desc) => query.OrderByDescending(game => game.Name),
			(GameSortBy.CreatedAt, SortDirection.Desc) => query.OrderByDescending(game => game.CreatedAt),
			(GameSortBy.UpdatedAt, SortDirection.Asc) => query.OrderBy(game => game.UpdatedAt),
			(GameSortBy.UpdatedAt, SortDirection.Desc) => query.OrderByDescending(game => game.UpdatedAt),
			_ => query.OrderBy(game => game.CreatedAt)
		};

		var categories = from category in context.Categories
						 join categoryGroup in context.CategoryGroups on category.CategoryGroupId equals categoryGroup.Id
						 join map in context.Maps on categoryGroup.MapId equals map.Id
						 where category.Status == PublicationStatus.Published && categoryGroup.Status == PublicationStatus.Published
							 && map.Status == PublicationStatus.Published
						 select new { category.Id, map.GameId };

		var rows = await ordered.ThenBy(game => game.Id).Skip(request.Offset).Take(request.Limit)
			.Select(game => new
			{
				Game = game,
				MapsCount = context.Maps.Count(map => map.GameId == game.Id && map.Status == PublicationStatus.Published),
				CategoriesCount = categories.Count(category => category.GameId == game.Id),
				MarkersCount = context.Markers.Count(marker => marker.Status == PublicationStatus.Published
					&& categories.Any(category => category.Id == marker.CategoryId && category.GameId == game.Id))
			}).ToArrayAsync(cancellationToken);

		var ids = rows.SelectMany(row => new Guid?[] { row.Game.CoverId, row.Game.BannerId }).OfType<Guid>().Distinct().ToArray();

		var assets = await context.MediaAssets.OfType<ImageAsset>().AsNoTracking()
			.Where(asset => ids.Contains(asset.Id)).ToDictionaryAsync(asset => asset.Id, asset => asset.ToDto(), cancellationToken);

		var items = rows.Select(row => row.Game.ToPublicCardDto(
			assets[row.Game.CoverId], row.Game.BannerId is { } bannerId ? assets[bannerId] : null,
			row.MapsCount, row.MarkersCount, row.CategoriesCount)).ToArray();

		return new PaginatedResult<PublicGameCardDto>(items, request.Limit, request.Offset, total);
	}
}
