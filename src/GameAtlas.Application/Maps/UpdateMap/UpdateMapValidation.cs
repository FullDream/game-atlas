using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.UpdateMap;

public sealed class UpdateMapValidation : AbstractValidator<UpdateMap>
{
	public UpdateMapValidation()
	{
		RuleFor(command => command).Must(command => command.Name.IsSpecified)
			.WithErrorCode(MapOperationErrors.EmptyPatch.Code).WithMessage(MapOperationErrors.EmptyPatch.Message)
			.OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(Map.NameMaxLength),
			value => value == null ? null : value.Trim());
	}
}
