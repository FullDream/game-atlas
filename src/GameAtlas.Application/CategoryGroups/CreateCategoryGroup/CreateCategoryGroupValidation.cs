using FluentValidation;
using GameAtlas.Application.Common.Validation;

namespace GameAtlas.Application.CategoryGroups.CreateCategoryGroup;

public sealed class CreateCategoryGroupValidation : AbstractValidator<CreateCategoryGroup>
{
	public CreateCategoryGroupValidation()
	{
		RuleFor(command => command.Name == null ? null : command.Name.Trim())
			.NotEmpty().MaximumLength(CategoryGroupLimits.NameMaxLength)
			.OverridePropertyName(nameof(CreateCategoryGroup.Name));

		this.OptionalRuleFor(command => command.Order, rule => rule.NotNull().GreaterThan(0));
	}
}
