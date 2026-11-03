using FluentValidation;
using GameAtlas.Domain.Categories;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;

namespace GameAtlas.Application.Categories.Groups.UpdateCategoryGroup;

public sealed class UpdateCategoryGroupValidation : AbstractValidator<UpdateCategoryGroup>
{
	public UpdateCategoryGroupValidation()
	{
		RuleFor(command => command).Must(command => Optional.AnySpecified(command.Name, command.BeforeId, command.IconAssetId, command.Color))
			.WithErrorCode(UpdateCategoryGroupErrors.EmptyPatch.Code)
			.WithMessage(UpdateCategoryGroupErrors.EmptyPatch.Message).OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(CategoryGroupLimits.NameMaxLength),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Color,
			rule => rule.NotEmpty().Length(4, 7).Matches(Category.ColorPattern));
	}
}
