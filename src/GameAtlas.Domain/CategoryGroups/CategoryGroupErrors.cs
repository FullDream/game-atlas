using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.CategoryGroups;

public static class CategoryGroupErrors
{
	public static readonly Error NameRequired = new(
		"CategoryGroup.Name.Required", "Name must not be empty.", nameof(CategoryGroup.Name), ErrorType.Validation);

	public static readonly Error InvalidOrder = new(
		"CategoryGroup.Order.Invalid", "Order must be a positive integer.",
		nameof(CategoryGroup.Order), ErrorType.Validation);
}
