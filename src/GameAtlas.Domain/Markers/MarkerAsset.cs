namespace GameAtlas.Domain.Markers;

public sealed class MarkerAsset
{
	internal MarkerAsset(Guid markerId, Guid assetId, int order)
	{
		MarkerId = markerId;
		AssetId = assetId;
		Order = order;
	}

	public Guid MarkerId { get; }
	public Guid AssetId { get; }
	public int Order { get; private set; }

	internal void SetOrder(int order) => Order = order;
}
