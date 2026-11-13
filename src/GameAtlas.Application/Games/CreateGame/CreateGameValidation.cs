using GameAtlas.Domain.ValueObjects;
using GameAtlas.Application.Common.Validation;
using FluentValidation;

namespace GameAtlas.Application.Games.CreateGame;

public sealed class CreateGameValidation : AbstractValidator<CreateGame>
{
	public CreateGameValidation()
	{
		RuleFor(command => command.Name == null ? null : command.Name.Trim())
			.NotEmpty()
			.MaximumLength(GameMetadataLimits.NameMaxLength)
			.OverridePropertyName(nameof(CreateGame.Name));

		RuleFor(command => command.Slug == null ? null : command.Slug.Trim())
			.ValidSlug()
			.OverridePropertyName(nameof(CreateGame.Slug));

		RuleFor(command => command.CoverId).NotNull().NotEqual(Guid.Empty);
	}
}
