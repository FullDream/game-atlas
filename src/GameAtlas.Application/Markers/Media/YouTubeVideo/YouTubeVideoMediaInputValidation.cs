using FluentValidation;
using GameAtlas.Domain.Markers.Media;

namespace GameAtlas.Application.Markers.Media.YouTubeVideo;

public sealed class YouTubeVideoMediaInputValidation : AbstractValidator<YouTubeVideoMediaInput>
{
	public YouTubeVideoMediaInputValidation() =>
		RuleFor(input => input.VideoId).NotEmpty().Length(11).Matches(YouTubeVideoMedia.VideoIdPattern);
}
