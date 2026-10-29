using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Processing;
using Microsoft.AspNetCore.SignalR;

namespace GameAtlas.Api.Maps.Events;

public sealed class MapNotifications(IHubContext<MapHub, IMapHubClient> hub, ILogger<MapNotifications> logger)
	: IMapNotifications
{
	public Task StyleUpdatedAsync(Guid mapId, MapStyleDto style) =>
		SendAsync(mapId, client => client.StyleUpdated(style));

	public Task ProcessingUpdatedAsync(MapProcessingDto processing) =>
		SendAsync(processing.MapId, client => client.ProcessingUpdated(processing));

	private async Task SendAsync(Guid mapId, Func<IMapHubClient, Task> send)
	{
		try
		{
			await send(hub.Clients.Group(MapHub.Group(mapId))).WaitAsync(TimeSpan.FromSeconds(5));
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Could not send map notification for {MapId}", mapId);
		}
	}
}
