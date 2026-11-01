using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Maps;

public static class MapErrors
{
	public static readonly Error InvalidDimensions = new(
		"Map.Dimensions.Invalid", "Width and height must be between 1 and 16384.", "Dimensions", ErrorType.Validation);

	public static readonly Error InvalidDefaultStyle = new(
		"Map.DefaultStyle.Invalid", "Choose a ready style as default.", "DefaultStyleId", ErrorType.Validation);

	public static readonly Error InvalidStyleOrder = new(
		"Map.StyleOrder.Invalid", "Supply every current style exactly once.", "StyleOrder", ErrorType.Validation);

	public static readonly Error NameConflict = new(
		"Map.Style.Name.Conflict", "A style with this name already exists.", "Name", ErrorType.Conflict);

	public static readonly Error InvalidName = new(
		"Map.Style.Name.Invalid", "Name must contain between 1 and 200 characters.", "Name", ErrorType.Validation);

	public static readonly Error InvalidColor = new(
		"Map.Style.Color.Invalid", "BackgroundColor must use #RGB or #RRGGBB.",
		"BackgroundColor", ErrorType.Validation);

	public static readonly Error InvalidMapName = new(
		"Map.Name.Invalid", "Name must contain between 1 and 200 characters.", "Name", ErrorType.Validation);

	public static readonly Error MissingStyle = new(
		"Map.Style.NotFound", "The style was not found.", Type: ErrorType.NotFound);

	public static readonly Error StyleOwnershipMismatch = new(
		"Map.Style.Ownership.Invalid", "The style belongs to another map.", "StyleId", ErrorType.Validation);

	public static readonly Error DuplicateStyle = new(
		"Map.Style.Id.Conflict", "This style has already been added to the map.", "StyleId", ErrorType.Conflict);
}
