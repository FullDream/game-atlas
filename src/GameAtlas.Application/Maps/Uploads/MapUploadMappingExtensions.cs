using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.Uploads;

internal static class MapUploadMappingExtensions
{
	internal static MapStyleInput ToDomain(this StagedMapUpload upload) => new(
		upload.Metadata.Name,
		upload.Metadata.BackgroundColor,
		upload.Image.Format,
		upload.Image.Width,
		upload.Image.Height,
		upload.Image.ByteLength);
}
