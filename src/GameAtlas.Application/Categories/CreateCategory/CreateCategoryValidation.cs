using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain.Categories;

namespace GameAtlas.Application.Categories.CreateCategory;

public sealed class CreateCategoryValidation : AbstractValidator<CreateCategory>
{
	public CreateCategoryValidation()
	{
		RuleFor(command => command.Name == null ? null : command.Name.Trim())
			.NotEmpty().MaximumLength(CategoryLimits.NameMaxLength).OverridePropertyName(nameof(CreateCategory.Name));

		RuleFor(command => command.IconAssetId).NotNull().NotEqual(Guid.Empty);

		RuleFor(command => command.Color).NotEmpty().Length(4, 7).Matches(Category.ColorPattern);
		this.OptionalRuleFor(command => command.Order, rule => rule.NotNull().GreaterThan(0));
	}
}
