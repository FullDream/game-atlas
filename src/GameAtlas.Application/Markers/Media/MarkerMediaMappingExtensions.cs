using GameAtlas.Application.Markers.Media.Image;
using GameAtlas.Application.Markers.Media.Video;
using GameAtlas.Application.Markers.Media.YouTubeVideo;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;
using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Markers.Media;

internal static class MarkerMediaMappingExtensions
{
	internal static MarkerMediaDto ToDto(this MarkerMedia media) => media switch
	{
		ImageMedia image => new ImageMediaDto(image.Url, image.Width, image.Height, image.Alt),
		YouTubeVideoMedia video => new YouTubeVideoMediaDto(video.VideoId),
		VideoMedia video => new VideoMediaDto(video.Url, video.Preview),
		_ => throw new InvalidOperationException("Unmapped marker media type.")
	};

	internal static Result<IReadOnlyCollection<MarkerMedia>> ToDomain(
		this IReadOnlyCollection<MarkerMediaInput?>? inputs)
	{
		if (inputs is null)
			return MarkerErrors.InvalidMedia;

		return inputs.Traverse(ToDomain);
	}

	private static Result<MarkerMedia> ToDomain(MarkerMediaInput? input) => input switch
	{
		ImageMediaInput image => AsMedia(ImageMedia.Create(image.Url, image.Width ?? 0, image.Height ?? 0, image.Alt)),
		YouTubeVideoMediaInput video => AsMedia(YouTubeVideoMedia.Create(video.VideoId)),
		VideoMediaInput video => AsMedia(VideoMedia.Create(video.Url, video.Preview)),
		_ => MarkerErrors.InvalidMedia
	};

	private static Result<MarkerMedia> AsMedia<T>(Result<T> result) where T : MarkerMedia =>
		result.IsFailure ? Result<MarkerMedia>.Failure(result.Errors) : Result<MarkerMedia>.Success(result.Value);
}
