using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Markers.CreateMarker;

public static class CreateMarkerErrors
{
	public static readonly Error CategoryNotFound = new(
		"Category.NotFound", "The category was not found in this game.",
		nameof(CreateMarker.CategoryId), ErrorType.NotFound);
}
