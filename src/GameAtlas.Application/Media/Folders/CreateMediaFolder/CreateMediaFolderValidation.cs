using FluentValidation;
using GameAtlas.Domain.Media;

namespace GameAtlas.Application.Media.Folders.CreateMediaFolder;

public sealed class CreateMediaFolderValidation : AbstractValidator<CreateMediaFolder>
{
	public CreateMediaFolderValidation() =>
		RuleFor(command => command.Name == null ? null : command.Name.Trim())
			.NotEmpty().MaximumLength(MediaFolder.NameMaxLength)
			.OverridePropertyName(nameof(CreateMediaFolder.Name));
}
