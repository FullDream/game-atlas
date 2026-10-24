using FluentValidation;
using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.CreateMap;

public sealed class CreateMapValidation : AbstractValidator<CreateMap>
{
	public CreateMapValidation()
	{
		RuleFor(command => (command.Name ?? string.Empty).Trim()).NotEmpty().MaximumLength(Map.NameMaxLength)
			.OverridePropertyName(nameof(CreateMap.Name));

		RuleFor(command => command.Width).NotNull().GreaterThan(0).LessThanOrEqualTo(MapGeometry.MaximumDimension);
		RuleFor(command => command.Height).NotNull().GreaterThan(0).LessThanOrEqualTo(MapGeometry.MaximumDimension);
	}
}
