using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Media;

public abstract class MediaAsset
{
	private protected MediaAsset(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid? FolderId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string ContentType { get; private protected set; } = string.Empty;
	public long ByteLength { get; private protected set; }
	public DateTimeOffset CreatedAt { get; private protected set; }
	public DateTimeOffset UpdatedAt { get; private protected set; }

	public Result<bool> Update(Optional<string?> name = default, Optional<Guid?> folderId = default,
		Optional<string?> alt = default, Optional<Guid?> previewAssetId = default)
	{
		var nextName = name.GetValueOrDefault(Name)?.Trim();
		var nextFolder = folderId.GetValueOrDefault(FolderId);

		if (string.IsNullOrEmpty(nextName) || nextName.Length > MediaFolder.NameMaxLength)
			return MediaErrors.InvalidName;

		if (alt.IsSpecified && (this is not ImageAsset || alt.Value is null))
			return alt.Value is null ? MediaErrors.AltRequired : MediaErrors.IncompatibleMetadata;

		if (previewAssetId.IsSpecified && this is not VideoAsset)
			return MediaErrors.IncompatibleMetadata;

		var changed = nextName != Name || nextFolder != FolderId;

		if (this is ImageAsset image && alt.IsSpecified && image.Alt != alt.Value)
		{
			image.SetAlt(alt.Value ?? string.Empty);
			changed = true;
		}

		if (this is VideoAsset video && previewAssetId.IsSpecified && video.PreviewAssetId != previewAssetId.Value)
		{
			video.SetPreview(previewAssetId.Value);
			changed = true;
		}

		if (!changed)
			return false;

		Name = nextName;
		FolderId = nextFolder;
		UpdatedAt = DateTimeOffset.UtcNow;

		return true;
	}
}
