using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;
using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.UpdateMapStyle;

public sealed class UpdateMapStyleValidation : AbstractValidator<UpdateMapStyle>
{
	public UpdateMapStyleValidation()
	{
		RuleFor(command => command).Must(command => Optional.AnySpecified(command.Name, command.BackgroundColor))
			.WithErrorCode(MapOperationErrors.EmptyPatch.Code).WithMessage(MapOperationErrors.EmptyPatch.Message)
			.OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(MapStyle.NameMaxLength),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.BackgroundColor,
			rule => rule.NotEmpty().Matches(MapStyle.ColorPattern));
	}
}
