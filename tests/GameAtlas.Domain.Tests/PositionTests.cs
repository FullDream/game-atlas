using GameAtlas.Domain.Markers;

namespace GameAtlas.Domain.Tests;

public class PositionTests
{
	[Theory]
	[InlineData(0, 0)]
	[InlineData(123.45, -67.89)]
	[InlineData(-1, 1000000)]
	public void FiniteCoordinatesHaveValueEquality(double x, double y)
	{
		var position = Assert.IsType<Position>(Position.Create(x, y).Value);

		Assert.Equal(x, position.X);
		Assert.Equal(y, position.Y);
		Assert.Equal(position, Position.Create(x, y).Value);
		Assert.All(typeof(Position).GetProperties(), property => Assert.Null(property.SetMethod));
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteCoordinatesAreRejected(double invalid)
	{
		Assert.True(Position.Create(invalid, 0).IsFailure);
		Assert.True(Position.Create(0, invalid).IsFailure);
	}
}
