using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;
using GameAtlas.Domain.Categories;

namespace GameAtlas.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryValidation : AbstractValidator<UpdateCategory>
{
	public UpdateCategoryValidation()
	{
		RuleFor(command => command).Must(command => Optional.AnySpecified(
				command.Name, command.Icon, command.Color, command.Order, command.CategoryGroupId))
			.WithErrorCode(UpdateCategoryErrors.EmptyPatch.Code)
			.WithMessage(UpdateCategoryErrors.EmptyPatch.Message).OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(CategoryLimits.NameMaxLength),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Icon,
			rule => rule.NotEmpty().MaximumLength(CategoryLimits.IconMaxLength),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Color,
			rule => rule.NotEmpty().Length(4, 7).Matches(Category.ColorPattern));

		this.OptionalRuleFor(command => command.Order, rule => rule.NotNull().GreaterThan(0));
		this.OptionalRuleFor(command => command.CategoryGroupId, rule => rule.NotNull());
	}
}
