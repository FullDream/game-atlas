using GameAtlas.Api.Markers.Media;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Markers.CreateMarker;
using GameAtlas.Application.Markers.Media;
using GameAtlas.Application.Markers.Media.Image;
using GameAtlas.Application.Markers.Media.Video;
using GameAtlas.Application.Markers.Media.YouTubeVideo;
using GameAtlas.Application.Markers.UpdateMarker;
using GameAtlas.Domain;

namespace GameAtlas.Api.Markers;

internal static class MarkerMappingExtensions
{
	internal static CreateMarker ToCommand(this CreateMarkerRequest request, Guid gameId) =>
		new(gameId, request.CategoryId, request.Name, request.Position?.ToInput(), request.Description,
			request.Media?.Select(item => item.ToInput()).ToArray());

	internal static UpdateMarker ToCommand(this UpdateMarkerRequest request, Guid gameId, Guid id) =>
		new(gameId, id, request.CategoryId, request.Name, request.Description,
			request.Position.IsSpecified
				? Optional<PositionPatch?>.From(request.Position.Value?.ToInput())
				: default,
			request.Media.IsSpecified
				? Optional<IReadOnlyCollection<MarkerMediaInput?>?>.From(
					request.Media.Value?.Select(item => item.ToInput()).ToArray())
				: default);

	private static PositionInput ToInput(this PositionRequest request) => new(request.X, request.Y);

	private static PositionPatch ToInput(this PositionPatchRequest request) => new(request.X, request.Y);

	private static MarkerMediaInput? ToInput(this MarkerMediaRequest? request) => request switch
	{
		ImageMediaRequest image => new ImageMediaInput(image.Url, image.Width, image.Height, image.Alt),
		YouTubeVideoMediaRequest video => new YouTubeVideoMediaInput(video.VideoId),
		VideoMediaRequest video => new VideoMediaInput(video.Url, video.Preview),
		null => null,
		_ => throw new InvalidOperationException("Unsupported marker media request.")
	};
}
