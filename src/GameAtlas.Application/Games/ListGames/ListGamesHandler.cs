using GameAtlas.Domain.Media;
using GameAtlas.Domain.Maps;
using GameAtlas.Application.Media;
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
			_ => query.OrderBy(game => game.CreatedAt)
		};

		query = ordered.ThenBy(game => game.Id);

		var games = await query.Skip(request.Offset).Take(request.Limit)
			.ToArrayAsync(cancellationToken);

		if (games.Length == 0)
			return new PaginatedResult<GameDto>([], request.Limit, request.Offset, total);

		var ids = games.Select(game => game.Id).ToArray();

		var maps = await context.Maps.AsNoTracking().Where(map => ids.Contains(map.GameId))
			.OrderBy(map => map.CreatedAt).ThenBy(map => map.Id)
			.Select(map => new
			{
				map.Id,
				map.GameId,
				map.Name,
				PreviewId = context.MapStyles.Where(style => style.MapId == map.Id && style.Id == map.DefaultStyleId && style.Status == MapProcessingStatus.Ready)
				 .OrderBy(style => style.CreationSequence).Select(style => (Guid?)style.SourceAssetId).FirstOrDefault()
			})
			.ToArrayAsync(cancellationToken);

		var categories = from category in context.Categories
						 join categoryGroup in context.CategoryGroups on category.CategoryGroupId equals categoryGroup.Id
						 join map in context.Maps on categoryGroup.MapId equals map.Id
						 where ids.Contains(map.GameId)
						 select new { category.Id, map.GameId };

		var categoryCounts = await categories.GroupBy(item => item.GameId)
			.Select(group => new { GameId = group.Key, Count = group.Count() })
			.ToDictionaryAsync(item => item.GameId, item => item.Count, cancellationToken);

		var markerCounts = await (from marker in context.Markers
								  join category in categories on marker.CategoryId equals category.Id
								  select new { category.GameId }).GroupBy(item => item.GameId)
			.Select(group => new { GameId = group.Key, Count = group.Count() })
			.ToDictionaryAsync(item => item.GameId, item => item.Count, cancellationToken);

		var assetIds = games.SelectMany(game => new Guid?[] { game.CoverId, game.BannerId })
			.Concat(maps.Select(map => map.PreviewId)).OfType<Guid>().Distinct().ToArray();

		var assets = await context.MediaAssets.OfType<ImageAsset>().AsNoTracking().Where(asset => assetIds.Contains(asset.Id))
			.ToDictionaryAsync(asset => asset.Id, asset => asset.ToDto(), cancellationToken);

		var summaries = maps.ToLookup(map => map.GameId);

		// Restrictive foreign keys protect these references; missing original files retain their asset metadata.
		var items = games.Select(game =>
		{
			var gameMaps = summaries[game.Id].Select(map => new GameMapDto(map.Id, map.Name,
				map.PreviewId is { } previewId ? assets[previewId] : null)).ToArray();

			return game.ToDto(assets[game.CoverId], game.BannerId is { } bannerId ? assets[bannerId] : null,
				markerCounts.GetValueOrDefault(game.Id), categoryCounts.GetValueOrDefault(game.Id), gameMaps);
		}).ToArray();

		return new PaginatedResult<GameDto>(items,
			request.Limit, request.Offset, total);
	}
}
