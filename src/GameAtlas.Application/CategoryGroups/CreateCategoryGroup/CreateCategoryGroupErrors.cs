using GameAtlas.Domain.Result;

namespace GameAtlas.Application.CategoryGroups.CreateCategoryGroup;

public static class CreateCategoryGroupErrors
{
	public static readonly Error ExplicitOrderRequired = new(
		"CategoryGroup.Order.Required", "The order range is exhausted. Supply an explicit positive order.",
		nameof(CreateCategoryGroup.Order), ErrorType.Validation);

	public static readonly Error MapNotFound = new(
		"Map.NotFound", "The map was not found.", nameof(CreateCategoryGroup.MapId), ErrorType.NotFound);

	public static readonly Error NameConflict = new(
		"CategoryGroup.Name.Conflict", "A category group with this name already exists in this map.",
		nameof(CreateCategoryGroup.Name), ErrorType.Conflict);
}
