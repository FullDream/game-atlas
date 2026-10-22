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
			.NotEmpty()
			.MaximumLength(GameMetadataLimits.SlugMaxLength)
			.Matches(@"\A[a-z0-9]+(-[a-z0-9]+)*\z")
			.OverridePropertyName(nameof(CreateGame.Slug));

		RuleFor(command => command.Image == null ? null : command.Image.Trim())
			.NotEmpty()
			.MaximumLength(GameMetadataLimits.ImageMaxLength)
			.OverridePropertyName(nameof(CreateGame.Image));
	}
}
