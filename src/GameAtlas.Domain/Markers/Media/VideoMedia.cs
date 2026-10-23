using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers.Media;

public sealed class VideoMedia : MarkerMedia
{
	private VideoMedia(Guid id, string url, string? preview) : base(id)
	{
		Url = url;
		Preview = preview;
	}

	public string Url { get; }
	public string? Preview { get; }

	public static Result<VideoMedia> Create(string? url, string? preview = null)
	{
		url = url?.Trim();
		preview = string.IsNullOrWhiteSpace(preview) ? null : preview.Trim();

		if (!IsValidReference(url) || url is null)
			return MarkerMediaErrors.InvalidUrl;

		if (preview?.Length > MarkerLimits.ReferenceMaxLength)
			return MarkerMediaErrors.InvalidPreview;

		return new VideoMedia(Guid.CreateVersion7(), url, preview);
	}

	internal override bool HasSameContent(MarkerMedia other) =>
		other is VideoMedia video && Url == video.Url && Preview == video.Preview;
}
