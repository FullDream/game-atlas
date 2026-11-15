using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Media;
using GameAtlas.Domain;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Result;
using GameAtlas.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Application.Maps.GetPublicMap;

public sealed class GetPublicMapHandler(IApplicationDbContext context)
	: IQueryHandler<GetPublicMap, Result<PublicMapDetailsDto>>
{
	public async ValueTask<Result<PublicMapDetailsDto>> Handle(GetPublicMap request, CancellationToken cancellationToken)
	{
		var gameSlug = Slug.Create(request.GameSlug);
		var mapSlug = Slug.Create(request.MapSlug);
		var includeDrafts = request.Visibility == ContentVisibility.IncludeDrafts;

		if (gameSlug.IsFailure || mapSlug.IsFailure)
			return MapOperationErrors.NotFound;

		await using var transaction = await context.BeginTransactionAsync(cancellationToken);

		var gameIds = context.Games.Where(game => game.Slug == gameSlug.Value!
			&& (includeDrafts || game.Status == PublicationStatus.Published)).Select(game => game.Id);

		var map = await context.Maps.AsNoTracking().Where(map => gameIds.Contains(map.GameId)
			&& map.Slug == mapSlug.Value! && (includeDrafts || map.Status == PublicationStatus.Published))
			.SingleOrDefaultAsync(cancellationToken);

		if (map is null)
			return MapOperationErrors.NotFound;

		var styles = await context.MapStyles.AsNoTracking().Where(style => style.MapId == map.Id
			&& style.Status == MapProcessingStatus.Ready).Include(style => style.Source)
			.OrderBy(style => style.DisplayOrder).ThenBy(style => style.CreationSequence).ToArrayAsync(cancellationToken);

		var groupQuery = context.CategoryGroups.AsNoTracking().Where(group => group.MapId == map.Id
			&& (includeDrafts || group.Status == PublicationStatus.Published));

		var categoryQuery = context.Categories.AsNoTracking().Where(category => groupQuery.Any(group => group.Id == category.CategoryGroupId)
			&& (includeDrafts || category.Status == PublicationStatus.Published));

		var groups = await groupQuery.OrderBy(group => group.Order).ThenBy(group => group.Id).ToArrayAsync(cancellationToken);
		var categories = await categoryQuery.OrderBy(category => category.Order).ThenBy(category => category.Id).ToArrayAsync(cancellationToken);

		var markers = await context.Markers.AsNoTracking().Where(marker => categoryQuery.Any(category => category.Id == marker.CategoryId)
			&& (includeDrafts || marker.Status == PublicationStatus.Published))
			.OrderBy(marker => marker.Id).Include(marker => marker.Media).ToArrayAsync(cancellationToken);

		var assetIds = markers.SelectMany(marker => marker.Media).Select(item => (Guid?)item.AssetId)
			.Concat(groups.Select(group => group.IconAssetId)).Concat(categories.Select(category => category.IconAssetId))
			.OfType<Guid>().Select(id => (Guid?)id).Distinct();

		var keys = assetIds.Select(id => id!.Value).ToArray();

		var assets = await context.MediaAssets.AsNoTracking().Where(asset => keys.Contains(asset.Id))
			.WithPreview().ToArrayAsync(cancellationToken);

		var result = map.ToPublicDto(styles, groups, categories, markers,
			assets.ToDictionary(asset => asset.Id, asset => asset.ToDto()));
		await transaction.CommitAsync(cancellationToken);

		return result;
	}
}
