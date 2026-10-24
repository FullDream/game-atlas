using FluentValidation;
using GameAtlas.Application.Maps.Uploads;

namespace GameAtlas.Application.Maps.AddMapStyle;

public sealed class AddMapStyleValidation : AbstractValidator<AddMapStyle>
{
	public AddMapStyleValidation()
	{
		RuleFor(command => command.Upload.Metadata).SetValidator(new MapUploadMetadataValidation())
			.OverridePropertyName(string.Empty);
	}
}
