using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories.CreateCategory;

public static class CreateCategoryErrors
{
	public static readonly Error GroupNotFound = new(
		"CategoryGroup.NotFound", "The category group was not found.",
		nameof(CreateCategory.GroupId), ErrorType.NotFound);

	public static readonly Error NameConflict = new(
		"Category.Name.Conflict", "A category with this name already exists in this group.",
		nameof(CreateCategory.Name), ErrorType.Conflict);

}
