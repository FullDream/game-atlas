using FluentValidation;

namespace GameAtlas.Application.Games.ListGames;

public sealed class ListGamesValidation : AbstractValidator<ListGames>
{
	public ListGamesValidation()
	{
		RuleFor(query => query.Offset).GreaterThanOrEqualTo(0);
		RuleFor(query => query.Limit).InclusiveBetween(1, 100);
		RuleFor(query => query.SortBy)
			.Must(value => value is null || IsOneOf(value, "name", "createdAt", "updatedAt"))
			.WithErrorCode(ListGamesErrors.InvalidSortBy.Code)
			.WithMessage(ListGamesErrors.InvalidSortBy.Message);

		RuleFor(query => query.SortBy)
			.NotNull()
			.When(query => query.SortDirection is not null)
			.WithErrorCode(ListGamesErrors.SortByRequired.Code)
			.WithMessage(ListGamesErrors.SortByRequired.Message);

		RuleFor(query => query.SortDirection)
			.Must(value => value is null || IsOneOf(value, "asc", "desc"))
			.WithErrorCode(ListGamesErrors.InvalidSortDirection.Code)
			.WithMessage(ListGamesErrors.InvalidSortDirection.Message);
	}

	private static bool IsOneOf(string value, params string[] options) =>
		options.Contains(value, StringComparer.OrdinalIgnoreCase);
}
