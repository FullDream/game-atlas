using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Categories;

public static class CategoryGroupErrors
{
	public static readonly Error NameRequired = new(
		"CategoryGroup.Name.Required", "Name must not be empty.", nameof(CategoryGroup.Name), ErrorType.Validation);

	public static readonly Error InvalidColor = new(
		"CategoryGroup.Color.Invalid", "Color must be a hex color such as #FFF or #FFFFFF.",
		nameof(CategoryGroup.Color), ErrorType.Validation);

	public static readonly Error InvalidOrder = new(
		"CategoryGroup.Order.Invalid", "Order must be a positive integer.",
		nameof(CategoryGroup.Order), ErrorType.Validation);

	public static readonly Error StatusRequired = new(
		"CategoryGroup.Status.Required", "Status is required.", nameof(CategoryGroup.Status), ErrorType.Validation);

	public static readonly Error InvalidStatus = new(
		"CategoryGroup.Status.Invalid", "Status must be draft or published.", nameof(CategoryGroup.Status), ErrorType.Validation);
}
