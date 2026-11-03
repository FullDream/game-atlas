using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Categories;

internal static class CategoryPlacement
{
	internal static Result<int> GetIndex(IReadOnlyList<Guid> siblings, Guid id, Guid? beforeId)
	{
		if (beforeId == id)
			return CategoryPlacementErrors.SelfAnchor;

		if (beforeId is null)
			return siblings.Count;

		for (var index = 0; index < siblings.Count; index++)
		{
			if (siblings[index] == beforeId)
				return index;
		}

		return CategoryPlacementErrors.AnchorNotFound;
	}
}
