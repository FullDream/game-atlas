using System.Text.RegularExpressions;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers.Media;

public sealed class YouTubeVideoMedia : MarkerMedia
{
	public const string VideoIdPattern = @"^[A-Za-z0-9_-]{11}(?![\s\S])";
	private static readonly Regex VideoIdRegex = new(VideoIdPattern, RegexOptions.CultureInvariant);

	private YouTubeVideoMedia(Guid id, string videoId) : base(id) => VideoId = videoId;

	public string VideoId { get; }

	public static Result<YouTubeVideoMedia> Create(string? videoId)
	{
		if (videoId is null || !VideoIdRegex.IsMatch(videoId))
			return MarkerMediaErrors.InvalidVideoId;

		return new YouTubeVideoMedia(Guid.CreateVersion7(), videoId);
	}

	internal override bool HasSameContent(MarkerMedia other) =>
		other is YouTubeVideoMedia video && VideoId == video.VideoId;
}
