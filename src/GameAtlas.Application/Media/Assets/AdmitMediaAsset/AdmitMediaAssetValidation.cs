using FluentValidation;
using GameAtlas.Domain.Media;

namespace GameAtlas.Application.Media.Assets.AdmitMediaAsset;

public sealed class AdmitMediaAssetValidation : AbstractValidator<AdmitMediaAsset>
{
	public AdmitMediaAssetValidation() => RuleFor(command => command.Name == null ? null : command.Name.Trim())
		.NotEmpty().MaximumLength(MediaFolder.NameMaxLength)
		.OverridePropertyName(nameof(AdmitMediaAsset.Name));
}
