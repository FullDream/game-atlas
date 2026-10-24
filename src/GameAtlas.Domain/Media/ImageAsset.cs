using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Media;

public sealed class ImageAsset : MediaAsset
{
	private ImageAsset(Guid id) : base(id)
	{
	}

	public double? Width { get; private set; }
	public double? Height { get; private set; }
	public string Alt { get; private set; } = string.Empty;
	public string? Placeholder { get; private set; }
	public bool IsRaster => ContentType != "image/svg+xml";

	public static Result<ImageAsset> Create(Guid id, string? name, Guid? folderId, string contentType,
		long byteLength, double? width, double? height, string? alt = "", string? placeholder = null)
	{
		if (id == Guid.Empty || byteLength <= 0 || contentType is not
			("image/jpeg" or "image/png" or "image/webp" or "image/avif" or "image/svg+xml"))
			return MediaErrors.InvalidContent;

		if (!ValidDimension(width) || !ValidDimension(height) || contentType != "image/svg+xml" &&
			(width is null || height is null || width != Math.Truncate(width.Value) ||
			 height != Math.Truncate(height.Value)))
			return MediaErrors.InvalidDimensions;

		if (contentType == "image/svg+xml" && placeholder is not null)
			return MediaErrors.IncompatibleMetadata;

		var asset = new ImageAsset(id)
		{
			ContentType = contentType,
			ByteLength = byteLength,
			Width = width,
			Height = height,
			Placeholder = placeholder
		};
		var result = asset.Update(name, folderId, alt);

		if (result.IsFailure)
			return result.Errors;

		asset.CreatedAt = asset.UpdatedAt;

		return asset;
	}

	internal void SetAlt(string alt) => Alt = alt;

	private static bool ValidDimension(double? value) => value is null || value > 0 && double.IsFinite(value.Value);
}
