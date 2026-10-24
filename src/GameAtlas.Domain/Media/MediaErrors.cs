using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Media;

public static class MediaErrors
{
	public static readonly Error InvalidName = new(
		"Media.Name.Invalid", "Name must contain 1 through 200 trimmed characters.", "Name", ErrorType.Validation);

	public static readonly Error InvalidContent = new(
		"Media.Content.Invalid", "The file is not a supported valid media asset.", "File", ErrorType.Validation);

	public static readonly Error InvalidDimensions = new(
		"Media.Dimensions.Invalid", "Image dimensions must be positive and finite.", "File", ErrorType.Validation);

	public static readonly Error AltRequired = new(
		"Media.Alt.Required", "Alt must not be null.", "Alt", ErrorType.Validation);

	public static readonly Error IncompatibleMetadata = new(
		"Media.Metadata.Incompatible", "The supplied metadata is not valid for this asset type.", "File",
		ErrorType.Validation);

	public static readonly Error FolderCycle = new(
		"Media.Folder.Cycle", "A folder cannot contain itself or be moved into its descendants.", "ParentId",
		ErrorType.Validation);
}
