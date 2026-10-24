using FluentValidation;
using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.AddMapStyle;

public sealed class AddMapStyleValidation : AbstractValidator<AddMapStyle>
{
	public AddMapStyleValidation()
	{
		RuleFor(command => command.Name == null ? null : command.Name.Trim())
			.NotEmpty().MaximumLength(MapStyle.NameMaxLength).OverridePropertyName(nameof(AddMapStyle.Name));

		RuleFor(command => command.BackgroundColor).NotEmpty().Matches(MapStyle.ColorPattern);
		RuleFor(command => command.SourceAssetId).NotNull().NotEqual(Guid.Empty);
	}
}
