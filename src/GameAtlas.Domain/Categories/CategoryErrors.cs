using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Categories;

public static class CategoryErrors
{
	public static readonly Error NameRequired = new(
		"Category.Name.Required", "Name must not be empty.", nameof(Category.Name), ErrorType.Validation);


	public static readonly Error InvalidColor = new(
		"Category.Color.Invalid", "Color must use the #RGB or #RRGGBB hexadecimal format.",
		nameof(Category.Color), ErrorType.Validation);

	public static readonly Error InvalidOrder = new(
		"Category.Order.Invalid", "Order must be a positive integer.", nameof(Category.Order), ErrorType.Validation);

	public static readonly Error GroupRequired = new(
		"Category.CategoryGroupId.Required", "CategoryGroupId must not be null.",
		nameof(Category.CategoryGroupId), ErrorType.Validation);

	public static readonly Error StatusRequired = new(
		"Category.Status.Required", "Status is required.", nameof(Category.Status), ErrorType.Validation);

	public static readonly Error InvalidStatus = new(
		"Category.Status.Invalid", "Status must be draft or published.", nameof(Category.Status), ErrorType.Validation);
}
