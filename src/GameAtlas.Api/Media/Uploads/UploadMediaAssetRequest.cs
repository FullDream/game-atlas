namespace GameAtlas.Api.Media.Uploads;

public sealed record UploadMediaAssetRequest(
	IFormFile File,
	Guid? FolderId = null,
	string? Name = null,
	string? Alt = null,
	Guid? PreviewAssetId = null);
