using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers.Media;

public sealed class ImageMedia : MarkerMedia
{
	private ImageMedia(Guid id, string url, int width, int height, string alt) : base(id)
	{
		Url = url;
		Width = width;
		Height = height;
		Alt = alt;
	}

	public string Url { get; }
	public int Width { get; }
	public int Height { get; }
	public string Alt { get; }

	public static Result<ImageMedia> Create(string? url, int width, int height, string? alt = "")
	{
		url = url?.Trim();

		if (!IsValidReference(url) || url is null)
			return MarkerMediaErrors.InvalidUrl;

		if (width <= 0 || height <= 0)
			return MarkerMediaErrors.InvalidDimensions;

		if (alt is null)
			return MarkerMediaErrors.AltRequired;

		return new ImageMedia(Guid.CreateVersion7(), url, width, height, alt);
	}

	internal override bool HasSameContent(MarkerMedia other) =>
		other is ImageMedia image && Url == image.Url && Width == image.Width &&
		Height == image.Height && Alt == image.Alt;
}
