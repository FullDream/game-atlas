using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Result;

namespace GameAtlas.IntegrationTests;

internal sealed class MediaFaultStorage(IMediaStorage inner, MapStorageFaults faults) : IMediaStorage
{
	public string PublicAssetsRoot => inner.PublicAssetsRoot;

	public string AssetPath(Guid assetId) => inner.AssetPath(assetId);
	public string StagingPath(Guid uploadId) => inner.StagingPath(uploadId);

	public Task<Result<long>> WriteAsync(Guid uploadId, Stream source, long maximumBytes,
		CancellationToken cancellationToken) =>
		inner.WriteAsync(uploadId, source, maximumBytes, cancellationToken);

	public void Promote(Guid uploadId, Guid assetId)
	{
		faults.PromotionCount++;

		if (faults.PromotionCount == faults.FailPromotionNumber && !faults.FailAfterPromotion)
			throw new IOException("Injected media promotion failure.");

		inner.Promote(uploadId, assetId);

		if (faults.PromotionCount == faults.FailPromotionNumber && faults.FailAfterPromotion)
			throw new IOException("Injected failure after media promotion.");
	}

	public void DeleteStaging(Guid uploadId)
	{
		if (faults.FailCleanup)
			throw new IOException("Injected media cleanup failure.");

		inner.DeleteStaging(uploadId);
	}

	public void DeleteAsset(Guid assetId)
	{
		if (faults.FailCleanup)
			throw new IOException("Injected media cleanup failure.");

		inner.DeleteAsset(assetId);
	}
}
