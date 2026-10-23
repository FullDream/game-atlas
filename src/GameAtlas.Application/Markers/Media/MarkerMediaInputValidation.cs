using FluentValidation;
using GameAtlas.Application.Markers.Media.Image;
using GameAtlas.Application.Markers.Media.Video;
using GameAtlas.Application.Markers.Media.YouTubeVideo;

namespace GameAtlas.Application.Markers.Media;

public sealed class MarkerMediaInputValidation : AbstractValidator<MarkerMediaInput?>
{
	public MarkerMediaInputValidation() => RuleFor(input => input).SetInheritanceValidator(validator =>
	{
		validator.Add(new ImageMediaInputValidation());
		validator.Add(new YouTubeVideoMediaInputValidation());
		validator.Add(new VideoMediaInputValidation());
	});
}
