using FluentValidation;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers.Media.Image;

public sealed class ImageMediaInputValidation : AbstractValidator<ImageMediaInput>
{
	public ImageMediaInputValidation()
	{
		RuleFor(input => (input.Url ?? string.Empty).Trim())
			.NotEmpty().MaximumLength(MarkerLimits.ReferenceMaxLength).OverridePropertyName(nameof(ImageMediaInput.Url));

		RuleFor(input => input.Width).NotNull().GreaterThan(0);
		RuleFor(input => input.Height).NotNull().GreaterThan(0);
		RuleFor(input => input.Alt).NotNull();
	}
}
