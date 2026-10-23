using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories.ListCategories;

public static class ListCategoriesErrors
{
	public static readonly Error GroupNotFound = new(
		"CategoryGroup.NotFound", "The category group was not found.",
		nameof(ListCategories.GroupId), ErrorType.NotFound);
}
