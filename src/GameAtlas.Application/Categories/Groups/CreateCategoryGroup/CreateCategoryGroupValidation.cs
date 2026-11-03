using FluentValidation;
using GameAtlas.Domain.Categories;
using GameAtlas.Application.Common.Validation;

namespace GameAtlas.Application.Categories.Groups.CreateCategoryGroup;

public sealed class CreateCategoryGroupValidation : AbstractValidator<CreateCategoryGroup>
{
	public CreateCategoryGroupValidation()
	{
		RuleFor(command => command.Name == null ? null : command.Name.Trim())
			.NotEmpty().MaximumLength(CategoryGroupLimits.NameMaxLength)
			.OverridePropertyName(nameof(CreateCategoryGroup.Name));

		RuleFor(command => command.Color).NotEmpty().Length(4, 7).Matches(Category.ColorPattern);
	}
}
