namespace GameAtlas.Application.Media.Storage;

public sealed class MediaLimits
{
	public long MaximumBytes { get; set; } = 300L * 1024 * 1024;
	public int MaximumImageSide { get; set; } = 16384;
	public int PlaceholderSide { get; set; } = 32;
	public int MaximumPlaceholderBytes { get; set; } = 8192;
	public int MaximumXmlDepth { get; set; } = 64;
	public long MaximumXmlCharacters { get; set; } = 4L * 1024 * 1024;
	public int MaximumXmlElements { get; set; } = 100000;
	public int MaximumXmlAttributes { get; set; } = 64;
	public int ProbeTimeoutSeconds { get; set; } = 30;
	public long VideoInspectionBytes { get; set; } = 8L * 1024 * 1024;
	public int ConcurrentVideoInspections { get; set; } = 2;
}
