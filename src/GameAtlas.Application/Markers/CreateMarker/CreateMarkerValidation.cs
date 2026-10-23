using FluentValidation;
using GameAtlas.Application.Markers.Media;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers.CreateMarker;

public sealed class CreateMarkerValidation : AbstractValidator<CreateMarker>
{
	public CreateMarkerValidation()
	{
		RuleFor(command => command.CategoryId).NotNull();
		RuleFor(command => (command.Name ?? string.Empty).Trim())
			.NotEmpty().MaximumLength(MarkerLimits.NameMaxLength).OverridePropertyName(nameof(CreateMarker.Name));

		RuleFor(command => (command.Description ?? string.Empty).Trim())
			.MaximumLength(MarkerLimits.DescriptionMaxLength).OverridePropertyName(nameof(CreateMarker.Description));

		RuleFor(command => command.Position!).NotNull().SetValidator(new PositionInputValidation());
		RuleFor(command => command.Media).NotNull();
		RuleForEach(command => command.Media).NotNull().SetValidator(new MarkerMediaInputValidation());
	}
}
