using GameAtlas.Domain.Result;

namespace GameAtlas.Application.CategoryGroups.CreateCategoryGroup;

public static class CreateCategoryGroupErrors
{
	public static readonly Error ExplicitOrderRequired = new(
		"CategoryGroup.Order.Required", "The order range is exhausted. Supply an explicit positive order.",
		nameof(CreateCategoryGroup.Order), ErrorType.Validation);

	public static readonly Error GameNotFound = new(
		"Game.NotFound", "The game was not found.", nameof(CreateCategoryGroup.GameId), ErrorType.NotFound);

	public static readonly Error NameConflict = new(
		"CategoryGroup.Name.Conflict", "A category group with this name already exists in this game.",
		nameof(CreateCategoryGroup.Name), ErrorType.Conflict);
}
