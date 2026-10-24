using GameAtlas.Domain.Result;

namespace GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;

public static class UpdateCategoryGroupErrors
{
	public static readonly Error NotFound = new(
		"CategoryGroup.NotFound", "The category group was not found.", nameof(UpdateCategoryGroup.Id),
			ErrorType.NotFound);

	public static readonly Error EmptyPatch = new(
		"CategoryGroup.Update.Empty", "At least one mutable field must be supplied.", null, ErrorType.Validation);

	public static readonly Error NameConflict = new(
		"CategoryGroup.Name.Conflict", "A category group with this name already exists in this map.",
		nameof(UpdateCategoryGroup.Name), ErrorType.Conflict);
}
