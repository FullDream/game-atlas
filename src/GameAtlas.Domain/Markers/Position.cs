using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Markers;

public sealed record Position
{
	private Position(double x, double y)
	{
		X = x;
		Y = y;
	}

	public double X { get; }
	public double Y { get; }

	public static Result<Position> Create(double x, double y)
	{
		if (!double.IsFinite(x) || !double.IsFinite(y))
			return PositionErrors.InvalidCoordinates;

		return new Position(x, y);
	}
}
