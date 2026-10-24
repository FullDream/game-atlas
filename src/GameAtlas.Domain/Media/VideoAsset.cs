using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Media;

public sealed class VideoAsset : MediaAsset
{
	private VideoAsset(Guid id) : base(id)
	{
	}

	public Guid? PreviewAssetId { get; private set; }
	public ImageAsset? Preview { get; private set; }

	public static Result<VideoAsset> Create(Guid id, string? name, Guid? folderId, string contentType,
		long byteLength, Guid? previewAssetId = null)
	{
		if (id == Guid.Empty || byteLength <= 0 || contentType is not ("video/mp4" or "video/webm"))
			return MediaErrors.InvalidContent;

		var asset = new VideoAsset(id) { ContentType = contentType, ByteLength = byteLength };
		var result = asset.Update(name, folderId, previewAssetId: previewAssetId);

		if (result.IsFailure)
			return result.Errors;

		asset.CreatedAt = asset.UpdatedAt;

		return asset;
	}

	internal void SetPreview(Guid? id) => PreviewAssetId = id;
}
