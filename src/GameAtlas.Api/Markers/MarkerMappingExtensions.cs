using GameAtlas.Application.Markers;
using GameAtlas.Application.Markers.CreateMarker;
using GameAtlas.Application.Markers.UpdateMarker;
using GameAtlas.Domain;

namespace GameAtlas.Api.Markers;

internal static class MarkerMappingExtensions
{
	internal static CreateMarker ToCommand(this CreateMarkerRequest request, Guid gameId, Guid mapId) =>
		new(gameId, mapId, request.CategoryId, request.Name, request.Position?.ToInput(), request.Description,
			request.Media);

	internal static UpdateMarker ToCommand(this UpdateMarkerRequest request, Guid gameId, Guid mapId, Guid id) =>
		new(gameId, mapId, id, request.CategoryId, request.Name, request.Description,
			request.Position.IsSpecified
				? Optional<PositionPatch?>.From(request.Position.Value?.ToInput())
				: default,
			request.Media);

	private static PositionInput ToInput(this PositionRequest request) => new(request.X, request.Y);

	private static PositionPatch ToInput(this PositionPatchRequest request) => new(request.X, request.Y);

}
