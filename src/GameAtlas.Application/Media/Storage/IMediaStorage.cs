using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Media.Storage;

public interface IMediaStorage
{
	string PublicAssetsRoot { get; }

	string AssetPath(Guid assetId);
	string StagingPath(Guid uploadId);
	Task<Result<long>> WriteAsync(Guid uploadId, Stream source, long maximumBytes, CancellationToken cancellationToken);
	void Promote(Guid uploadId, Guid assetId);
	void DeleteStaging(Guid uploadId);
	void DeleteAsset(Guid assetId);
}
