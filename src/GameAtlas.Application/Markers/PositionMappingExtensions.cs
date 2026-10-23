using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Markers;

internal static class PositionMappingExtensions
{
	internal static Result<Position> ToDomain(this PositionInput? input)
	{
		if (input?.X is not { } x || input.Y is not { } y)
			return MarkerErrors.PositionRequired;

		return Position.Create(x, y);
	}

	internal static Result<Position> Merge(this PositionPatch? patch, Position current)
	{
		if (patch is null)
			return MarkerErrors.PositionRequired;

		var x = patch.X.GetValueOrDefault(current.X);
		var y = patch.Y.GetValueOrDefault(current.Y);

		if (x is null || y is null)
			return PositionErrors.InvalidCoordinates;

		return Position.Create(x.Value, y.Value);
	}
}
