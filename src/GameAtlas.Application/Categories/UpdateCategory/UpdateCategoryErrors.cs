using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories.UpdateCategory;

public static class UpdateCategoryErrors
{
	public static readonly Error NotFound = new(
		"Category.NotFound", "The category was not found.", nameof(UpdateCategory.Id), ErrorType.NotFound);

	public static readonly Error GroupNotFound = new(
		"CategoryGroup.NotFound", "The destination category group was not found.",
		nameof(UpdateCategory.CategoryGroupId), ErrorType.NotFound);

	public static readonly Error NameConflict = new(
		"Category.Name.Conflict", "A category with this name already exists in this group.",
		nameof(UpdateCategory.Name), ErrorType.Conflict);

	public static readonly Error EmptyPatch = new(
		"Category.Update.Empty", "At least one mutable field must be supplied.", null, ErrorType.Validation);
}
