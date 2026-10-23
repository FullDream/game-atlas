using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;

namespace GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;

public sealed class UpdateCategoryGroupValidation : AbstractValidator<UpdateCategoryGroup>
{
	public UpdateCategoryGroupValidation()
	{
		RuleFor(command => command).Must(command => Optional.AnySpecified(command.Name, command.Order))
			.WithErrorCode(UpdateCategoryGroupErrors.EmptyPatch.Code)
			.WithMessage(UpdateCategoryGroupErrors.EmptyPatch.Message).OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(CategoryGroupLimits.NameMaxLength),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Order, rule => rule.NotNull().GreaterThan(0));
	}
}
