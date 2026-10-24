using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Media.Inspection;

public interface IMediaInspector
{
	Task<Result<MediaInspection>> InspectAsync(string path, CancellationToken cancellationToken);
}
