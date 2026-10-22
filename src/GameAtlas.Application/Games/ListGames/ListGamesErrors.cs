using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Games.ListGames;

public static class ListGamesErrors
{
	public static readonly Error InvalidSortBy = new(
		"Games.SortBy.Invalid", "SortBy must be name, createdAt, or updatedAt.",
		nameof(ListGames.SortBy), ErrorType.Validation);

	public static readonly Error InvalidSortDirection = new(
		"Games.SortDirection.Invalid", "SortDirection must be asc or desc.",
		nameof(ListGames.SortDirection), ErrorType.Validation);

	public static readonly Error SortByRequired = new(
		"Games.SortBy.Required", "SortBy is required when SortDirection is supplied.",
		nameof(ListGames.SortBy), ErrorType.Validation);
}
