using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Maps;

public sealed record MapGeometry
{
	public const int TileSize = 512;
	public const int MaximumDimension = 16384;

	internal MapGeometry(int width, int height)
	{
		Width = width;
		Height = height;
	}

	public int Width { get; }
	public int Height { get; }
	public int MinZoom => 0;
	public int MaxZoom => Math.Max(0, (int)Math.Ceiling(Math.Log2(Math.Max(Width, Height) / (double)TileSize)));
	public double[] Extent => [-Width / 2d, -Height / 2d, Width / 2d, Height / 2d];
	public double[] TileOrigin => [-Width / 2d, Height / 2d];
	public double[] Resolutions => Enumerable.Range(0, MaxZoom + 1).Select(Resolution).ToArray();

	public static Result<MapGeometry> Create(int width, int height)
	{
		if (width is <= 0 or > MaximumDimension || height is <= 0 or > MaximumDimension)
			return MapErrors.InvalidDimensions;

		return new MapGeometry(width, height);
	}

	public int Columns(int zoom) => (int)Math.Ceiling(Width / (TileSize * Resolution(zoom)));
	public int Rows(int zoom) => (int)Math.Ceiling(Height / (TileSize * Resolution(zoom)));

	private double Resolution(int zoom) => Math.Pow(2, MaxZoom - zoom);
}
