using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Maps.Images;

public interface IMapImageProcessor
{
	Result<MapImageInfo> Probe(string originalPath);

	Task GenerateAsync(string originalPath, string outputDirectory, MapGeometry geometry, IProgress<int> progress,
		CancellationToken cancellationToken);

	void CheckCapabilities();
}
