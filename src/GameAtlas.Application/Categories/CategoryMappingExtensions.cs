using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Media;
using GameAtlas.Application.Media;

namespace GameAtlas.Application.Categories;

internal static class CategoryMappingExtensions
{
	internal static IQueryable<CategoryDto> ToDto(this IQueryable<Category> categories, IQueryable<Marker> markers,
		IQueryable<MediaAsset> assets) =>
		from category in categories
		join image in assets.OfType<ImageAsset>() on category.IconAssetId equals image.Id into iconImages
		from image in iconImages.DefaultIfEmpty()
		select new CategoryDto(
			category.Id,
			category.CategoryGroupId,
			category.Name,
			image == null ? null : new ImageAssetDto(
				image.Id, image.FolderId, image.Name,
				"/media/files/" + image.Id.ToString().ToLower(), image.ContentType,
				image.ByteLength, image.Width, image.Height,
				image.Alt, image.Placeholder, image.CreatedAt, image.UpdatedAt),
			category.Color,
			markers.Count(marker => marker.CategoryId == category.Id),
			category.CreatedAt,
			category.UpdatedAt);

	internal static CategoryDto ToDto(this Category category, int markersCount, ImageAssetDto? icon) => new(
		category.Id,
		category.CategoryGroupId,
		category.Name,
		icon,
		category.Color,
		markersCount,
		category.CreatedAt,
		category.UpdatedAt);
}
