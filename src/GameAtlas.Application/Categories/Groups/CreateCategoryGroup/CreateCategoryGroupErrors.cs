using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories.Groups.CreateCategoryGroup;

public static class CreateCategoryGroupErrors
{

	public static readonly Error MapNotFound = new(
		"Map.NotFound", "The map was not found.", nameof(CreateCategoryGroup.MapId), ErrorType.NotFound);

	public static readonly Error NameConflict = new(
		"CategoryGroup.Name.Conflict", "A category group with this name already exists in this map.",
		nameof(CreateCategoryGroup.Name), ErrorType.Conflict);
}
