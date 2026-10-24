using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Maps;

public static class MapOperationErrors
{
	public static readonly Error NotFound = new(
		"Map.NotFound", "The map or owning resource was not found.", Type: ErrorType.NotFound);

	public static readonly Error NameConflict = new(
		"Map.Name.Conflict", "A map with this name already exists in this game.", Type: ErrorType.Conflict);

	public static readonly Error EmptyPatch = new(
		"Map.Style.Patch.Empty", "At least one mutable field must be supplied.", Type: ErrorType.Validation);
}
