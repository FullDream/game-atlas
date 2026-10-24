using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Media;

public static class MediaOperationErrors
{
	public static readonly Error FolderNotFound = new(
		"Media.Folder.NotFound", "The media folder was not found.", "FolderId", ErrorType.NotFound);

	public static readonly Error AssetNotFound = new(
		"Media.Asset.NotFound", "The media asset was not found.", "AssetId", ErrorType.NotFound);

	public static readonly Error FolderNameConflict = new(
		"Media.Folder.Name.Conflict", "A sibling folder with this name already exists.", "Name", ErrorType.Conflict);

	public static readonly Error FolderNotEmpty = new(
		"Media.Folder.NotEmpty", "Move or delete the contents before deleting this folder.", "Id", ErrorType.Conflict);

	public static readonly Error AssetInUse = new(
		"Media.Asset.InUse", "The asset is referenced or currently being processed.", "Id", ErrorType.Conflict);

	public static readonly Error IncompatibleAsset = new(
		"Media.Asset.Incompatible", "The selected asset type is not permitted here.", "AssetId", ErrorType.Validation);

	public static readonly Error EmptyPatch = new(
		"Media.Patch.Empty", "At least one mutable field must be supplied.", "", ErrorType.Validation);
}
