using GameAtlas.Domain.Result;

namespace GameAtlas.Application.CategoryGroups.ListCategoryGroups;

public static class ListCategoryGroupsErrors
{
	public static readonly Error GameNotFound = new(
		"Game.NotFound", "The game was not found.", nameof(ListCategoryGroups.GameId), ErrorType.NotFound);
}
