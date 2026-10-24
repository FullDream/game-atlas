namespace GameAtlas.Infrastructure.Maps.Persistence;

internal sealed class MapAdmissionSession
{
	public Guid Id { get; set; } = Guid.CreateVersion7();
	public DateTimeOffset ExpiresAt { get; set; }
	public List<MapAdmissionStyle> Sources { get; set; } = [];
}
