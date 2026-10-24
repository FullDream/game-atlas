using FluentValidation;
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
		RuleForEach(command => command.Media).NotNull().NotEqual(Guid.Empty);
		RuleFor(command => command.Media).Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
			.WithMessage("Media asset IDs must be distinct.");
	}
}
