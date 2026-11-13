using FluentValidation;
using FluentValidation.Validators;
using GameAtlas.Domain.ValueObjects;

namespace GameAtlas.Application.Common.Validation;

public static class SlugValidation
{
	public static IRuleBuilderOptions<T, string?> ValidSlug<T>(this IRuleBuilder<T, string?> rule) =>
		rule.SetValidator(new SlugValidator<T>());
}

internal sealed class SlugValidator<T> : PropertyValidator<T, string?>
{
	public override string Name => "SlugValidator";

	public override bool IsValid(ValidationContext<T> context, string? value) => Slug.Create(value).IsSuccess;

	protected override string GetDefaultMessageTemplate(string errorCode) =>
		"Slug must contain 1..200 lowercase ASCII letters or digits separated by single hyphens.";
}
