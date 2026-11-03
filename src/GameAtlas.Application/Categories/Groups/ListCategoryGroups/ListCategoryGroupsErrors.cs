using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories.Groups.ListCategoryGroups;

public static class ListCategoryGroupsErrors
{
	public static readonly Error MapNotFound = new(
		"Map.NotFound", "The map was not found.", nameof(ListCategoryGroups.MapId), ErrorType.NotFound);
}
