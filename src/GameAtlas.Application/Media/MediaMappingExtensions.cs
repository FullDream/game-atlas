using GameAtlas.Domain.Media;

namespace GameAtlas.Application.Media;

public static class MediaMappingExtensions
{
	public static MediaFolderDto ToDto(this MediaFolder folder, int itemsCount) =>
		new(folder.Id, folder.ParentId, folder.Name, itemsCount, folder.CreatedAt, folder.UpdatedAt);

	public static ImageAssetDto ToDto(this ImageAsset image) =>
		new(image.Id, image.FolderId, image.Name, FileUrl(image.Id), image.ContentType, image.ByteLength,
			image.Width, image.Height, image.Alt, image.Placeholder, image.CreatedAt, image.UpdatedAt);

	public static MediaAssetDto ToDto(this MediaAsset asset) => asset switch
	{
		ImageAsset image => image.ToDto(),
		VideoAsset video => new VideoAssetDto(video.Id, video.FolderId, video.Name, FileUrl(video.Id),
			video.ContentType, video.ByteLength, video.Preview?.ToDto(), video.CreatedAt, video.UpdatedAt),
		_ => throw new InvalidOperationException("Unsupported media asset type.")
	};

	private static string FileUrl(Guid id) => $"/media/files/{id}";
}
