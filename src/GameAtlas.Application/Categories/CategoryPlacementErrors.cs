using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories;

internal static class CategoryPlacementErrors
{
	internal static readonly Error SelfAnchor = new("Category.Placement.Self", "An item cannot be placed before itself.", "BeforeId", ErrorType.Validation);
	internal static readonly Error AnchorNotFound = new("Category.Placement.NotFound", "The destination sibling was not found.", "BeforeId", ErrorType.NotFound);
}
