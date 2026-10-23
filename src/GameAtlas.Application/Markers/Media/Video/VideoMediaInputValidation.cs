using FluentValidation;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers.Media.Video;

public sealed class VideoMediaInputValidation : AbstractValidator<VideoMediaInput>
{
	public VideoMediaInputValidation()
	{
		RuleFor(input => (input.Url ?? string.Empty).Trim())
			.NotEmpty().MaximumLength(MarkerLimits.ReferenceMaxLength).OverridePropertyName(nameof(VideoMediaInput.Url));

		RuleFor(input => (input.Preview ?? string.Empty).Trim())
			.MaximumLength(MarkerLimits.ReferenceMaxLength).OverridePropertyName(nameof(VideoMediaInput.Preview));
	}
}
