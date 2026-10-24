using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Application.Markers.UpdateMarker;

public sealed class UpdateMarkerValidation : AbstractValidator<UpdateMarker>
{
	public UpdateMarkerValidation()
	{
		RuleFor(command => command).Must(command => Optional.AnySpecified(command.CategoryId, command.Name,
				command.Description, command.Position, command.Media))
			.WithErrorCode(UpdateMarkerErrors.EmptyPatch.Code)
			.WithMessage(UpdateMarkerErrors.EmptyPatch.Message).OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.CategoryId, rule => rule.NotNull());
		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(MarkerLimits.NameMaxLength),
			value => (value ?? string.Empty).Trim());

		this.OptionalRuleFor(command => command.Description,
			rule => rule.MaximumLength(MarkerLimits.DescriptionMaxLength),
			value => (value ?? string.Empty).Trim());

		When(command => command.Position.IsSpecified, () =>
		{
			RuleFor(command => command.Position.Value!).NotNull().SetValidator(new PositionPatchValidation())
				.OverridePropertyName(nameof(UpdateMarker.Position));
		});

		When(command => command.Media.IsSpecified, () =>
		{
			RuleFor(command => command.Media.Value).NotNull().OverridePropertyName(nameof(UpdateMarker.Media));
			RuleForEach(command => command.Media.Value).NotNull().NotEqual(Guid.Empty)
				.OverridePropertyName(nameof(UpdateMarker.Media));
			RuleFor(command => command.Media.Value).Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
				.WithMessage("Media asset IDs must be distinct.").OverridePropertyName(nameof(UpdateMarker.Media));
		});
	}
}
