using System.Data.Common;
using System.Net;
using System.Text;
using System.Text.Json;
using GameAtlas.Infrastructure.Maps.Processing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapEventStreamTests
{
	[Fact]
	public async Task EmptyMapStreamObservesAddedAndRemovedStylesAndClosesForItsOwnDeletion()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "empty-stream");
		var map = await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		using var response = await client.GetAsync($"{MapTestData.PathFor(game.Id, map.Id)}/events",
			HttpCompletionOption.ResponseHeadersRead, timeout.Token);
		using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token));
		var snapshot = await ReadEventAsync(reader, timeout.Token);

		Assert.Equal("snapshot", snapshot.Event);
		Assert.Empty(snapshot.Data.GetProperty("styles").EnumerateArray());
		Assert.Equal(JsonValueKind.Null, snapshot.Data.GetProperty("defaultStyleId").ValueKind);
		Assert.Equal("heartbeat", (await ReadEventAsync(reader, timeout.Token)).Event);
		using var upload = MapTestData.Style();
		using var added = await client.PostAsync($"{MapTestData.PathFor(game.Id, map.Id)}/styles", upload);
		var styled = await MapTestData.ReadAsync(added);
		var change = await ReadEventAsync(reader, timeout.Token);

		Assert.Equal(styled.Styles.Single().Id, change.Data.GetProperty("styles")[0].GetProperty("id").GetGuid());
		using var removed = await client.DeleteAsync(
			$"{MapTestData.PathFor(game.Id, map.Id)}/styles/{styled.Styles.Single().Id}");
		var empty = await ReadEventAsync(reader, timeout.Token);

		Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
		Assert.Empty(empty.Data.GetProperty("styles").EnumerateArray());
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id, map.Id));
		var terminal = await ReadEventAsync(reader, timeout.Token);

		Assert.Equal("mapDeleted", terminal.Event);
		Assert.Equal(map.Id, terminal.Data.GetProperty("mapId").GetGuid());
		Assert.Null(await reader.ReadLineAsync(timeout.Token));
	}

	[Fact]
	public async Task SubscribersSeeCurrentSnapshotsChangesHeartbeatAndPinnedMapDeletion()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = map.Styles.Single();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		using var firstCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
		using var first = await client.GetAsync($"{MapTestData.PathFor(game.Id)}/events",
			HttpCompletionOption.ResponseHeadersRead, firstCancellation.Token);
		using var second = await client.GetAsync($"{MapTestData.PathFor(game.Id)}/events",
			HttpCompletionOption.ResponseHeadersRead, timeout.Token);
		using var firstReader = new StreamReader(await first.Content.ReadAsStreamAsync(timeout.Token));
		using var secondReader = new StreamReader(await second.Content.ReadAsStreamAsync(timeout.Token));
		var firstSnapshot = await ReadEventAsync(firstReader, timeout.Token);
		var secondSnapshot = await ReadEventAsync(secondReader, timeout.Token);

		Assert.Equal("text/event-stream", first.Content.Headers.ContentType!.MediaType);
		Assert.Equal("snapshot", firstSnapshot.Event);
		Assert.Equal("snapshot", secondSnapshot.Event);
		Assert.Equal(map.Id, firstSnapshot.Data.GetProperty("mapId").GetGuid());
		Assert.Equal("queued", firstSnapshot.Data.GetProperty("styles")[0].GetProperty("status").GetString());
		Assert.Equal("no", Assert.Single(first.Headers.GetValues("X-Accel-Buffering")));
		using var patch = await client.PatchAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}",
			new StringContent("{\"name\":\"Roads\"}", Encoding.UTF8, "application/json"));

		Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
		Assert.Equal("Roads", (await ReadEventAsync(firstReader, timeout.Token)).Data
			.GetProperty("styles")[0].GetProperty("name").GetString());
		Assert.Equal("Roads", (await ReadEventAsync(secondReader, timeout.Token)).Data
			.GetProperty("styles")[0].GetProperty("name").GetString());
		firstCancellation.Cancel();
		first.Dispose();
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(timeout.Token));
		var progress = await ReadEventAsync(secondReader, timeout.Token);

		while (progress.Event == "progress" &&
			progress.Data.GetProperty("styles")[0].GetProperty("status").GetString() != "ready")
			progress = await ReadEventAsync(secondReader, timeout.Token);

		Assert.Equal("progress", progress.Event);
		Assert.Equal("ready", progress.Data.GetProperty("styles")[0].GetProperty("status").GetString());
		Assert.Equal(100, progress.Data.GetProperty("styles")[0].GetProperty("progress").GetInt32());
		using var reconnectRequest = new HttpRequestMessage(HttpMethod.Get, $"{MapTestData.PathFor(game.Id)}/events");

		reconnectRequest.Headers.Add("Last-Event-ID", "old-event");
		using var reconnect = await client.SendAsync(reconnectRequest, HttpCompletionOption.ResponseHeadersRead,
			timeout.Token);
		using var reconnectReader = new StreamReader(await reconnect.Content.ReadAsStreamAsync(timeout.Token));
		var reconnectSnapshot = await ReadEventAsync(reconnectReader, timeout.Token);

		Assert.Equal("snapshot", reconnectSnapshot.Event);
		Assert.Equal("ready", reconnectSnapshot.Data.GetProperty("styles")[0].GetProperty("status").GetString());
		var heartbeat = await ReadEventAsync(secondReader, timeout.Token);

		Assert.Equal("heartbeat", heartbeat.Event);
		Assert.Equal(map.Id, heartbeat.Data.GetProperty("mapId").GetGuid());
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id), timeout.Token);
		using var nextUpload = MapTestData.Style();
		using var recreated = await MapTestData.UploadFirstStyleAsync(client, game.Id, nextUpload);
		var nextMap = await MapTestData.ReadAsync(recreated);
		var terminal = await ReadEventAsync(secondReader, timeout.Token);

		Assert.NotEqual(map.Id, nextMap.Id);
		Assert.Equal("mapDeleted", terminal.Event);
		Assert.Equal(map.Id, terminal.Data.GetProperty("mapId").GetGuid());
		Assert.Null(await secondReader.ReadLineAsync(timeout.Token));
		timeout.Cancel();
	}

	[Fact]
	public async Task MissingMapReturnsProblemDetailsBeforeHeaders()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync($"{MapTestData.PathFor(Guid.NewGuid())}/events");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
	}

	[Fact]
	public async Task FailureAfterHeadersProducesSafeTerminalEvent()
	{
		var fault = new StreamReadFault();
		await using var factory = new GameApiFactory(interceptor: fault);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var response = await client.GetAsync($"{MapTestData.PathFor(game.Id)}/events",
			HttpCompletionOption.ResponseHeadersRead, timeout.Token);
		using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token));

		Assert.Equal("snapshot", (await ReadEventAsync(reader, timeout.Token)).Event);
		fault.Enabled = true;
		var error = await ReadEventAsync(reader, timeout.Token);

		Assert.Equal("error", error.Event);
		Assert.Equal(map.Id, error.Data.GetProperty("mapId").GetGuid());
		Assert.DoesNotContain("private", error.Data.GetRawText());
		Assert.Null(await reader.ReadLineAsync(timeout.Token));
	}

	internal static async Task<(string Event, JsonElement Data)> ReadEventAsync(StreamReader reader,
		CancellationToken cancellationToken)
	{
		var eventName = "message";
		var data = new StringBuilder();

		while (await reader.ReadLineAsync(cancellationToken) is { } line)
		{
			if (line.StartsWith("event:"))
				eventName = line[6..].Trim();
			else if (line.StartsWith("data:"))
				data.Append(line[5..].TrimStart());
			else if (line.Length == 0 && data.Length > 0)
				return (eventName, JsonDocument.Parse(data.ToString()).RootElement.Clone());
		}

		throw new EndOfStreamException("The stream ended before a complete event.");
	}

	private sealed class StreamReadFault : DbCommandInterceptor
	{
		public bool Enabled { get; set; }

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
			CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			if (Enabled && command.CommandText.Contains("FROM \"Maps\""))
				throw new InvalidOperationException("private database diagnostics");

			return ValueTask.FromResult(result);
		}
	}
}
