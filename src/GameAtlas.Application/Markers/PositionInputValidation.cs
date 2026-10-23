using FluentValidation;

namespace GameAtlas.Application.Markers;

public sealed class PositionInputValidation : AbstractValidator<PositionInput>
{
	public PositionInputValidation()
	{
		RuleFor(input => input.X).NotNull().Must(value => value is null || double.IsFinite(value.Value));
		RuleFor(input => input.Y).NotNull().Must(value => value is null || double.IsFinite(value.Value));
	}
}
