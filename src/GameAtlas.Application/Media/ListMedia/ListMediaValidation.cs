using FluentValidation;

namespace GameAtlas.Application.Media.ListMedia;

public sealed class ListMediaValidation : AbstractValidator<ListMedia>
{
	public ListMediaValidation()
	{
		RuleFor(request => request.Offset).GreaterThanOrEqualTo(0);
		RuleFor(request => request.Limit).InclusiveBetween(1, 100);
	}
}
