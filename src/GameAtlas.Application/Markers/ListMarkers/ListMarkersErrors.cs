using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Markers.ListMarkers;

public static class ListMarkersErrors
{
	public static readonly Error NotFound = new(
		"Game.NotFound", "The game was not found.",
		nameof(ListMarkers.GameId), ErrorType.NotFound);
}
