using GameAtlas.Domain.Media;
using GameAtlas.Domain.Maps;
using GameAtlas.Application.Media;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.GetGame;

public sealed class GetGameHandler(IApplicationDbContext context) : IQueryHandler<GetGame, Result<GameDto>>
{
	public async ValueTask<Result<GameDto>> Handle(GetGame request, CancellationToken cancellationToken)
	{
		var game = await context.Games.AsNoTracking()
			.SingleOrDefaultAsync(game => game.Id == request.Id, cancellationToken);

		if (game is null)
			return GetGameErrors.NotFound;

		var maps = await context.Maps.AsNoTracking().Where(map => map.GameId == game.Id)
			.OrderBy(map => map.CreatedAt).ThenBy(map => map.Id)
			.Select(map => new
			{
				map.Id,
				map.Name,
				PreviewId = context.MapStyles.Where(style => style.MapId == map.Id && style.Status == MapProcessingStatus.Ready)
				 .OrderBy(style => style.CreationSequence).Select(style => (Guid?)style.SourceAssetId).FirstOrDefault()
			})
			.ToArrayAsync(cancellationToken);

		var categoryIds = from category in context.Categories
						  join categoryGroup in context.CategoryGroups on category.CategoryGroupId equals categoryGroup.Id
						  join map in context.Maps on categoryGroup.MapId equals map.Id
						  where map.GameId == game.Id
						  select category.Id;

		var categoriesCount = await categoryIds.CountAsync(cancellationToken);

		var markersCount = await context.Markers.CountAsync(
			marker => categoryIds.Contains(marker.CategoryId), cancellationToken);

		var assetIds = new[] { (Guid?)game.CoverId, game.BannerId }
			.Concat(maps.Select(map => map.PreviewId)).OfType<Guid>().Distinct().ToArray();

		var assets = await context.MediaAssets.OfType<ImageAsset>().AsNoTracking().Where(asset => assetIds.Contains(asset.Id))
			.ToDictionaryAsync(asset => asset.Id, asset => asset.ToDto(), cancellationToken);

		// Restrictive foreign keys protect these references; missing original files retain their asset metadata.
		var gameMaps = maps.Select(map => new GameMapDto(map.Id, map.Name,
			map.PreviewId is { } previewId ? assets[previewId] : null)).ToArray();

		return game.ToDto(assets[game.CoverId], game.BannerId is { } bannerId ? assets[bannerId] : null,
			markersCount, categoriesCount, gameMaps);
	}
}
