using FluentValidation;
using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.Uploads;

public sealed class MapUploadMetadataValidation : AbstractValidator<MapUploadMetadata>
{
	public MapUploadMetadataValidation()
	{
		RuleFor(input => (input.Name ?? string.Empty).Trim()).NotEmpty().MaximumLength(MapStyle.NameMaxLength)
			.OverridePropertyName(nameof(MapUploadMetadata.Name));

		RuleFor(input => input.BackgroundColor).NotEmpty().Matches(MapStyle.ColorPattern);
	}
}
