namespace GameAtlas.Application.Maps.Storage;

public sealed class MapUploadLimits
{
	public long FileBytes { get; init; } = 200L * 1024 * 1024;
	public int MaximumDimension { get; init; } = 16384;
	public int MetadataBytes { get; init; } = 64 * 1024;
	public int EnvelopeBytes { get; init; } = 1024 * 1024;
}
