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
				command.Name, command.Slug, command.CoverId, command.Status, command.BannerId, command.ReleaseDate, command.Developer, command.Description))
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

		this.OptionalRuleFor(command => command.CoverId, rule => rule.NotNull().NotEqual(Guid.Empty));

		this.OptionalRuleFor(command => command.Status, rule => rule.NotNull().IsInEnum());
	}
}
