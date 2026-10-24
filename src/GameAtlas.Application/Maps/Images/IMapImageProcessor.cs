using GameAtlas.Domain.Maps;

namespace GameAtlas.Application.Maps.Images;

public interface IMapImageProcessor
{
	Task GenerateAsync(string originalPath, string outputDirectory, MapGeometry geometry, IProgress<int> progress,
		CancellationToken cancellationToken);

	void CheckCapabilities();
}
