using GameAtlas.Application.Maps.Uploads;

namespace GameAtlas.Api.Maps.Uploads;

internal sealed record MapStyleUploadReadResult(StagedMapUpload? Upload, IResult? Failure = null);
