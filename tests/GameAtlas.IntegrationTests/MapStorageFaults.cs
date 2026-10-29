namespace GameAtlas.IntegrationTests;

internal sealed class MapStorageFaults
{
	public int PromotionCount { get; set; }
	public int FailPromotionNumber { get; init; }
	public bool FailAfterPromotion { get; init; }
	public bool FailAfterPublicationMove { get; set; }
	public bool FailCleanup { get; set; }
	public Guid? CleanupStyleId { get; set; }
}
