using GameAtlas.Domain;
using GameAtlas.Domain.ValueObjects;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Maps;
using GameAtlas.Application.Media;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Games.GetPublicGame;

public sealed class GetPublicGameHandler(IApplicationDbContext context) : IQueryHandler<GetPublicGame, Result<PublicGameDto>>
{
	public async ValueTask<Result<PublicGameDto>> Handle(GetPublicGame request, CancellationToken cancellationToken)
	{
		var slug = Slug.Create(request.Slug);

		if (slug.IsFailure)
			return GetPublicGameErrors.NotFound;

		var game = await context.Games.AsNoTracking()
			.SingleOrDefaultAsync(game => game.Slug == slug.Value! && game.Status == PublicationStatus.Published, cancellationToken);

		if (game is null)
			return GetPublicGameErrors.NotFound;

		var maps = await context.Maps.AsNoTracking().Where(map => map.GameId == game.Id && map.Status == PublicationStatus.Published)
			.OrderBy(map => map.CreatedAt).ThenBy(map => map.Id)
			.Select(map => new
			{
				map.Id,
				map.Name,
				map.Slug,
				map.Status,
				PreviewId = context.MapStyles.Where(style => style.MapId == map.Id && style.Id == map.DefaultStyleId && style.Status == MapProcessingStatus.Ready)
				 .OrderBy(style => style.CreationSequence).Select(style => (Guid?)style.SourceAssetId).FirstOrDefault()
			})
			.ToArrayAsync(cancellationToken);

		var visibleCategories = from category in context.Categories
								join categoryGroup in context.CategoryGroups on category.CategoryGroupId equals categoryGroup.Id
								join map in context.Maps on categoryGroup.MapId equals map.Id
								where map.GameId == game.Id && map.Status == PublicationStatus.Published
							  && categoryGroup.Status == PublicationStatus.Published && category.Status == PublicationStatus.Published
								select new { category.Id, MapId = map.Id };

		var categoryCounts = await visibleCategories.GroupBy(category => category.MapId)
			.ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

		var visibleMarkers = from marker in context.Markers
							 join category in visibleCategories on marker.CategoryId equals category.Id
							 where marker.Status == PublicationStatus.Published
							 select new { category.MapId };

		var markerCounts = await visibleMarkers.GroupBy(marker => marker.MapId)
			.ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

		var assetIds = new[] { (Guid?)game.CoverId, game.BannerId }
			.Concat(maps.Select(map => map.PreviewId)).OfType<Guid>().Distinct().ToArray();

		var assets = await context.MediaAssets.OfType<ImageAsset>().AsNoTracking().Where(asset => assetIds.Contains(asset.Id))
			.ToDictionaryAsync(asset => asset.Id, asset => asset.ToDto(), cancellationToken);

		// Restrictive foreign keys protect these references; missing original files retain their asset metadata.
		var gameMaps = maps.Select(map => new PublicGameMapDto(map.Id, map.Name, map.Slug,
			map.PreviewId is { } previewId ? assets[previewId] : null,
			markerCounts.GetValueOrDefault(map.Id), categoryCounts.GetValueOrDefault(map.Id))).ToArray();

		return game.ToPublicDto(assets[game.CoverId], game.BannerId is { } bannerId ? assets[bannerId] : null,
			gameMaps.Sum(map => map.MarkersCount), gameMaps.Sum(map => map.CategoriesCount), gameMaps);
	}
}
