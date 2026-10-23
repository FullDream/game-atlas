namespace GameAtlas.Domain.Markers.Media;

public abstract class MarkerMedia
{
	private protected MarkerMedia(Guid id) => Id = id;

	public Guid Id { get; }
	public Guid MarkerId { get; private set; }
	public int Order { get; private set; }

	internal void AttachTo(Guid markerId, int order)
	{
		MarkerId = markerId;
		Order = order;
	}

	internal abstract bool HasSameContent(MarkerMedia other);

	protected static bool IsValidReference(string? value) =>
		!string.IsNullOrWhiteSpace(value) && value.Length <= MarkerLimits.ReferenceMaxLength;
}
