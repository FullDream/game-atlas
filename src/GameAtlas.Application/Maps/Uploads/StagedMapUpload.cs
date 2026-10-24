using GameAtlas.Application.Maps.Images;

namespace GameAtlas.Application.Maps.Uploads;

public sealed record StagedMapUpload(Guid FileId, MapUploadMetadata Metadata, MapImageInfo Image);
