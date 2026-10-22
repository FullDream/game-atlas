using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;

namespace GameAtlas.Application.Games.UpdateGame;

public sealed class UpdateGameValidation : AbstractValidator<UpdateGame>
{
	public UpdateGameValidation()
	{
		RuleFor(command => command)
			.Must(command => Optional.AnySpecified(
				command.Name, command.Slug, command.Image, command.MapId, command.Status))
			.WithErrorCode(UpdateGameErrors.EmptyPatch.Code)
			.WithMessage(UpdateGameErrors.EmptyPatch.Message)
			.OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(GameMetadataLimits.NameMaxLength),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Slug,
			rule => rule.NotEmpty().MaximumLength(GameMetadataLimits.SlugMaxLength)
				.Matches(@"\A[a-z0-9]+(-[a-z0-9]+)*\z"),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Image,
			rule => rule.NotEmpty().MaximumLength(GameMetadataLimits.ImageMaxLength),
			value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Status, rule => rule.NotNull().IsInEnum());
	}
}
