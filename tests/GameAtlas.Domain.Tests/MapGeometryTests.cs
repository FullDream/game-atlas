using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Markers;

namespace GameAtlas.Domain.Tests;

public class MapGeometryTests
{
	[Theory]
	[InlineData(1, 1, 0)]
	[InlineData(512, 512, 0)]
	[InlineData(513, 21, 1)]
	[InlineData(10000, 8000, 5)]
	[InlineData(16384, 16384, 5)]
	public void GeometryKeepsOriginalResolutionAndCenteredCoordinates(int width, int height, int maxZoom)
	{
		var geometry = MapGeometry.Create(width, height).Value!;

		Assert.Equal(maxZoom, geometry.MaxZoom);
		Assert.Equal(0, geometry.MinZoom);
		Assert.Equal([-width / 2d, -height / 2d, width / 2d, height / 2d], geometry.Extent);
		Assert.Equal([-width / 2d, height / 2d], geometry.TileOrigin);
		Assert.Equal(1d, geometry.Resolutions[^1]);
		Assert.Equal(1, geometry.Columns(0));
		Assert.Equal(1, geometry.Rows(0));

		for (var zoom = 0; zoom < maxZoom; zoom++)
			Assert.Equal(geometry.Resolutions[zoom + 1] * 2, geometry.Resolutions[zoom]);
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(1, -1)]
	[InlineData(16385, 512)]
	[InlineData(512, 16385)]
	public void InvalidDimensionsAreRejected(int width, int height) =>
		Assert.True(MapGeometry.Create(width, height).IsFailure);

	[Fact]
	public void OddRectangularTilesHavePreciseBoundsAndDoNotRestrictMarkers()
	{
		var geometry = MapGeometry.Create(1025, 513).Value!;

		Assert.Equal([-512.5, -256.5, 512.5, 256.5], geometry.Extent);
		Assert.Equal(3, geometry.Columns(2));
		Assert.Equal(2, geometry.Rows(2));
		Assert.True(Position.Create(-100000.125, 999999.5).IsSuccess);
	}
}
