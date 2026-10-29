using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace GameAtlas.IntegrationTests;

internal sealed class MapHubViewer : IAsyncDisposable
{
	private readonly Channel<JsonElement> events = Channel.CreateUnbounded<JsonElement>();
	private readonly Channel<JsonElement> styles = Channel.CreateUnbounded<JsonElement>();
	private readonly HubConnection connection;
	private Guid mapId;

	internal bool HasProcessingEvents => events.Reader.TryPeek(out _);
	internal bool HasStyleEvents => styles.Reader.TryPeek(out _);

	internal MapHubViewer(GameApiFactory factory)
	{
		connection = new HubConnectionBuilder().WithUrl("https://localhost/hubs/map", options =>
		{
			options.Transports = HttpTransportType.LongPolling;
			options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
		}).Build();

		connection.On<JsonElement>("ProcessingUpdated", data => events.Writer.TryWrite(data));
		connection.On<JsonElement>("StyleUpdated", data => styles.Writer.TryWrite(data));
	}

	internal async Task StartAsync(Guid mapId, CancellationToken cancellationToken)
	{
		this.mapId = mapId;
		await connection.StartAsync(cancellationToken);
		await connection.InvokeAsync("JoinMap", mapId, cancellationToken);
	}

	internal Task UnwatchAsync(CancellationToken cancellationToken) =>
		connection.InvokeAsync("LeaveMap", mapId, cancellationToken);

	internal Task JoinAsync(Guid mapId, CancellationToken cancellationToken) =>
		connection.InvokeAsync("JoinMap", mapId, cancellationToken);

	internal async Task<JsonElement> ReadAsync(CancellationToken cancellationToken) =>
		await events.Reader.ReadAsync(cancellationToken);

	internal async Task<JsonElement> ReadStyleAsync(CancellationToken cancellationToken) =>
		await styles.Reader.ReadAsync(cancellationToken);

	internal async Task<JsonElement> UntilAsync(Func<JsonElement, bool> predicate, CancellationToken cancellationToken)
	{
		while (true)
		{
			var item = await ReadAsync(cancellationToken);

			if (predicate(item))
				return item;
		}
	}

	internal async Task<JsonElement> UntilStyleAsync(Func<JsonElement, bool> predicate, CancellationToken cancellationToken)
	{
		while (true)
		{
			var item = await ReadStyleAsync(cancellationToken);

			if (predicate(item))
				return item;
		}
	}

	public ValueTask DisposeAsync() => connection.DisposeAsync();
}
