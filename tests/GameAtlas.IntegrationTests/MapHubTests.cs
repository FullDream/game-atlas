using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Api.Maps.Events;
using GameAtlas.Infrastructure.Maps.Processing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameAtlas.IntegrationTests;

public class MapHubTests
{
	[Fact]
	public async Task FailedNotificationsCannotFailCommittedStyleCreationOrProcessing()
	{
		var lifetime = new FailingLifetimeManager();
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<HubLifetimeManager<MapHub>>(lifetime));
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "send-failure");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);

		Assert.Equal(HttpStatusCode.OK, created.StatusCode);
		Assert.True(await factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(CancellationToken.None));
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var style = (await MapTestData.ReadAsync(read)).Styles.Single();

		Assert.Equal(GameAtlas.Domain.Maps.MapProcessingStatus.Ready, style.Status);
		Assert.NotNull(style.TilesUrlTemplate);
		Assert.True(lifetime.Failures > 0);
	}

	[Fact]
	public async Task OneConnectionCanJoinSeveralMapsAndLeaveOneWithoutLosingTheOther()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var firstGame = await UpdateGameHandlerTests.SeedAsync(factory, "first-group");
		var secondGame = await UpdateGameHandlerTests.SeedAsync(factory, "second-group");
		var firstMap = await MapTestData.EnsureMapAsync(factory, firstGame.Id);
		var secondMap = await MapTestData.EnsureMapAsync(factory, secondGame.Id);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		await using var first = new MapHubViewer(factory);
		await using var second = new MapHubViewer(factory);

		await first.StartAsync(firstMap.Id, timeout.Token);
		await first.JoinAsync(secondMap.Id, timeout.Token);
		await second.StartAsync(firstMap.Id, timeout.Token);
		await first.UnwatchAsync(timeout.Token);
		using var client = factory.CreateHttpsClient();
		using var firstUpload = MapTestData.Style();
		using var addedFirst = await MapTestData.UploadFirstStyleAsync(client, firstGame.Id, firstUpload);

		Assert.Equal(HttpStatusCode.OK, addedFirst.StatusCode);
		var firstResult = await MapTestData.ReadAsync(addedFirst);

		Assert.Equal(firstResult.Styles.Single().Id, (await second.ReadStyleAsync(timeout.Token)).GetProperty("id").GetGuid());
		using var secondUpload = MapTestData.Style();
		using var addedSecond = await MapTestData.UploadFirstStyleAsync(client, secondGame.Id, secondUpload);

		Assert.Equal(HttpStatusCode.OK, addedSecond.StatusCode);
		var style = await first.ReadStyleAsync(timeout.Token);
		var map = await MapTestData.ReadAsync(addedSecond);

		Assert.Equal(map.Styles.Single().Id, style.GetProperty("id").GetGuid());
	}

	[Fact]
	public async Task JoinSendsNoInitialSnapshotAndHttpProvidesCurrentState()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "empty");
		var map = await MapTestData.EnsureMapAsync(factory, game.Id);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await using var viewer = new MapHubViewer(factory);

		await viewer.StartAsync(map.Id, timeout.Token);
		await viewer.JoinAsync(map.Id, timeout.Token);
		using var client = factory.CreateHttpsClient();
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id), timeout.Token);

		Assert.Empty((await MapTestData.ReadAsync(read)).Styles);
		Assert.False(viewer.HasProcessingEvents);
		Assert.False(viewer.HasStyleEvents);
		await viewer.UnwatchAsync(timeout.Token);
		await viewer.UnwatchAsync(timeout.Token);
		using var old = await client.GetAsync($"{MapTestData.PathFor(game.Id)}/events", timeout.Token);

		Assert.Equal(HttpStatusCode.NotFound, old.StatusCode);
	}

	[Fact]
	public async Task IndependentViewersReceiveProgressAndCommittedStyleUpdates()
	{
		var images = new MapProcessingTests.ControlledImages { Pause = true };
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMapImageProcessor>(images));
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "viewers");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		await using var first = new MapHubViewer(factory);
		await using var second = new MapHubViewer(factory);

		await first.StartAsync(map.Id, timeout.Token);
		await second.StartAsync(map.Id, timeout.Token);
		await first.JoinAsync(map.Id, timeout.Token);
		var running = factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(timeout.Token);

		await images.Started.Task.WaitAsync(timeout.Token);
		var progress = await first.UntilAsync(data => data.GetProperty("progress").GetInt32() == 77, timeout.Token);

		Assert.Equal(map.Styles.Single().Id, progress.GetProperty("styleId").GetGuid());
		Assert.False(progress.TryGetProperty("styles", out _));
		Assert.False(progress.TryGetProperty("source", out _));
		Assert.NotEqual(Guid.Empty, progress.GetProperty("attemptId").GetGuid());
		var claimed = await first.ReadStyleAsync(timeout.Token);

		Assert.Equal("processing", claimed.GetProperty("status").GetString());
		Assert.False(first.HasStyleEvents);
		await first.UnwatchAsync(timeout.Token);
		using var patch = await client.PatchAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles/{map.Styles.Single().Id}",
			new { name = "Changed while processing" }, timeout.Token);

		Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
		var edited = await second.ReadStyleAsync(timeout.Token);

		while (edited.GetProperty("name").GetString() != "Changed while processing")
			edited = await second.ReadStyleAsync(timeout.Token);

		images.Release.TrySetResult();
		Assert.True(await running);
		var ready = await second.UntilStyleAsync(data => data.GetProperty("status").GetString() == "ready", timeout.Token);

		Assert.Equal(100, ready.GetProperty("progress").GetInt32());
		using var terminalPatch = await client.PatchAsJsonAsync(
			$"{MapTestData.PathFor(game.Id)}/styles/{map.Styles.Single().Id}",
			new { name = "Changed after ready" }, timeout.Token);

		Assert.Equal(HttpStatusCode.OK, terminalPatch.StatusCode);
		var afterReady = await second.ReadStyleAsync(timeout.Token);

		while (afterReady.GetProperty("name").GetString() != "Changed after ready")
			afterReady = await second.ReadStyleAsync(timeout.Token);

		Assert.NotNull(afterReady.GetProperty("tilesUrlTemplate").GetString());
		await using var reconnect = new MapHubViewer(factory);

		await reconnect.StartAsync(map.Id, timeout.Token);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id), timeout.Token);

		Assert.Equal(GameAtlas.Domain.Maps.MapProcessingStatus.Ready, (await MapTestData.ReadAsync(read)).Styles.Single().Status);
		Assert.False(reconnect.HasProcessingEvents);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MissingMapsCanBeJoinedAndAreReportedByHttp(bool empty)
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "scope");
		var mapId = empty ? Guid.Empty : Guid.NewGuid();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await using var viewer = new MapHubViewer(factory);

		await viewer.StartAsync(mapId, timeout.Token);
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync(MapTestData.PathFor(game.Id, mapId), timeout.Token);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.False(viewer.HasProcessingEvents);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MapAndGameDeletionNotifyTheirOwnGroups(bool deleteGame)
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "delete");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await using var viewer = new MapHubViewer(factory);

		await viewer.StartAsync(map.Id, timeout.Token);
		using var deleted = await client.DeleteAsync(deleteGame ? $"/games/{game.Id}" : MapTestData.PathFor(game.Id), timeout.Token);
		var terminal = await viewer.ReadAsync(timeout.Token);

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Equal(map.Id, terminal.GetProperty("mapId").GetGuid());
		Assert.Equal(System.Text.Json.JsonValueKind.Null, terminal.GetProperty("styleId").ValueKind);
		Assert.True(terminal.GetProperty("deleted").GetBoolean());
	}

	[Fact]
	public async Task StyleDeletionSendsATombstoneWithoutRemovingOtherStyles()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "delete-style");
		using var client = factory.CreateHttpsClient();
		using var created = await MapTestData.CreateWithStylesAsync(client, game.Id);
		var map = await MapTestData.ReadAsync(created);
		var styleId = map.Styles.First().Id;
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await using var viewer = new MapHubViewer(factory);

		await viewer.StartAsync(map.Id, timeout.Token);
		using var deleted = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{styleId}", timeout.Token);
		var notification = await viewer.ReadAsync(timeout.Token);

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.True(notification.GetProperty("deleted").GetBoolean());
		Assert.Equal(styleId, notification.GetProperty("styleId").GetGuid());
		using var remaining = await client.GetAsync(MapTestData.PathFor(game.Id), timeout.Token);

		Assert.Single((await MapTestData.ReadAsync(remaining)).Styles);
	}

	private sealed class FailingLifetimeManager()
		: DefaultHubLifetimeManager<MapHub>(NullLogger<DefaultHubLifetimeManager<MapHub>>.Instance)
	{
		public int Failures { get; private set; }

		public override Task SendGroupAsync(string groupName, string methodName, object?[] args,
			CancellationToken cancellationToken = default)
		{
			Failures++;

			return Task.FromException(new IOException("Injected notification failure."));
		}
	}
}
