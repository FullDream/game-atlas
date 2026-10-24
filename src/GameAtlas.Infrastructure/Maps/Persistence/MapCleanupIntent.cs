namespace GameAtlas.Infrastructure.Maps.Persistence;

internal sealed class MapCleanupIntent
{
	public Guid Id { get; set; } = Guid.CreateVersion7();
	public Guid? SessionId { get; set; }
	public Guid? MapId { get; set; }
	public Guid? StyleId { get; set; }
	public bool OutputsOnly { get; set; }
	public int Attempts { get; set; }
	public DateTimeOffset RetryAt { get; set; }
}
