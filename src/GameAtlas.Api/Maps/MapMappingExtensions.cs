using GameAtlas.Application.Maps.CreateMap;
using GameAtlas.Application.Maps.UpdateMapStyle;

namespace GameAtlas.Api.Maps;

internal static class MapMappingExtensions
{
	internal static CreateMap ToCommand(this CreateMapRequest request, Guid gameId) =>
		new(gameId, request.Name, request.Width, request.Height);

	internal static UpdateMapStyle ToCommand(this UpdateMapStyleRequest request, Guid gameId, Guid mapId,
		Guid styleId) =>
		new(gameId, mapId, styleId, request.Name, request.BackgroundColor);
}
