namespace GameAtlas.Application.Maps;

public sealed record MapTileGeometryDto(
	IReadOnlyCollection<double> Extent,
	IReadOnlyCollection<double> TileOrigin,
	int TileSize,
	int MinZoom,
	int MaxZoom,
	IReadOnlyCollection<double> Resolutions)
{
	public bool Equals(MapTileGeometryDto? other) => other is not null && TileSize == other.TileSize &&
		MinZoom == other.MinZoom && MaxZoom == other.MaxZoom && Extent.SequenceEqual(other.Extent) &&
		TileOrigin.SequenceEqual(other.TileOrigin) && Resolutions.SequenceEqual(other.Resolutions);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(TileSize);
		hash.Add(MinZoom);
		hash.Add(MaxZoom);

		foreach (var value in Extent.Concat(TileOrigin).Concat(Resolutions))
			hash.Add(value);

		return hash.ToHashCode();
	}
}
