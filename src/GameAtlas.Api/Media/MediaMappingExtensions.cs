using GameAtlas.Application.Media.Folders.CreateMediaFolder;
using GameAtlas.Application.Media.Folders.UpdateMediaFolder;

namespace GameAtlas.Api.Media;

internal static class MediaMappingExtensions
{
	internal static Application.Media.Assets.UpdateMediaAsset.UpdateMediaAsset ToCommand(
		this UpdateMediaAssetRequest request, Guid id) =>
		new(id, request.Name, request.FolderId, request.Alt, request.PreviewAssetId);

	internal static Application.Media.ListMedia.ListMedia ToQuery(this ListMediaRequest request) =>
		new(request.FolderId, request.Offset ?? 0, request.Limit ?? 20, request.Search);

	internal static CreateMediaFolder ToCommand(this CreateMediaFolderRequest request) =>
		new(request.Name, request.ParentId);

	internal static UpdateMediaFolder ToCommand(this UpdateMediaFolderRequest request, Guid id) =>
		new(id, request.Name, request.ParentId);
}
