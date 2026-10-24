namespace GameAtlas.Infrastructure.Maps.Persistence;

internal sealed class MapAdmissionStyle
{
	public Guid Id { get; set; } = Guid.CreateVersion7();
	public Guid SessionId { get; set; }
	public Guid MapId { get; set; }
	public Guid StyleId { get; set; }
}
