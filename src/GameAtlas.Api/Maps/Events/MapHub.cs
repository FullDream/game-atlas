using Microsoft.AspNetCore.SignalR;

namespace GameAtlas.Api.Maps.Events;

public sealed class MapHub : Hub<IMapHubClient>
{
	public Task JoinMap(Guid mapId) =>
		Groups.AddToGroupAsync(Context.ConnectionId, Group(mapId), Context.ConnectionAborted);

	public Task LeaveMap(Guid mapId) =>
		Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(mapId), Context.ConnectionAborted);

	internal static string Group(Guid mapId) => $"map:{mapId}";
}
