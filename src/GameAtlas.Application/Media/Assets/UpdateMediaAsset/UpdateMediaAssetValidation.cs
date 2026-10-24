using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;
using GameAtlas.Domain.Media;

namespace GameAtlas.Application.Media.Assets.UpdateMediaAsset;

public sealed class UpdateMediaAssetValidation : AbstractValidator<UpdateMediaAsset>
{
	public UpdateMediaAssetValidation()
	{
		RuleFor(command => command).Must(command =>
				Optional.AnySpecified(command.Name, command.FolderId, command.Alt, command.PreviewAssetId))
			.WithMessage(MediaOperationErrors.EmptyPatch.Message).OverridePropertyName(string.Empty);

		this.OptionalRuleFor(command => command.Name,
			rule => rule.NotEmpty().MaximumLength(MediaFolder.NameMaxLength), value => value == null ? null : value.Trim());

		this.OptionalRuleFor(command => command.Alt, rule => rule.NotNull());
	}
}
