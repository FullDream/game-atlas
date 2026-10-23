using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories.DeleteCategory;

public static class DeleteCategoryErrors
{
	public static readonly Error NotFound = new(
		"Category.NotFound", "The category was not found.", nameof(DeleteCategory.Id), ErrorType.NotFound);
}
