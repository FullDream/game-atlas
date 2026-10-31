using System.Collections.Concurrent;
using System.Data.Common;
using System.Net.Http.Json;
using GameAtlas.Application.Maps;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapQueryTests
{
	[Fact]
	public async Task ListingBreaksTimestampTiesByIdWithoutPerMapStyleQueries()
	{
		var capture = new SnapshotQueries();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "listing");
		using var client = factory.CreateHttpsClient();

		for (var index = 0; index < 10; index++)
		{
			using var created = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
				new { name = $"Map {index}", width = 23, height = 17 });
			var map = await MapTestData.ReadAsync(created);
			using var upload = MapTestData.Style();
			using var styled = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id, map.Id), upload);

			Assert.Single((await MapTestData.ReadAsync(styled)).Styles);
		}

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var instant = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

			await context.Maps.ExecuteUpdateAsync(setters => setters.SetProperty(map => map.CreatedAt, instant));
		}

		capture.Contexts.Clear();
		var maps = await client.GetFromJsonAsync<MapDto[]>(MapTestData.CollectionFor(game.Id), MapTestData.JsonOptions);

		Assert.Equal(10, maps!.Length);
		Assert.Equal(maps.Select(map => map.Id).Order(), maps.Select(map => map.Id));
		Assert.All(maps, map => Assert.Single(map.Styles));
		Assert.Equal(2, Assert.Single(capture.Contexts).Value);
		capture.Contexts.Clear();
		using var catalog = await client.GetAsync("/games");

		catalog.EnsureSuccessStatusCode();
		Assert.Equal(3, Assert.Single(capture.Contexts).Value);
	}

	[Fact]
	public async Task GroupMembershipDoesNotReadOrPollEvenForManyStyles()
	{
		var capture = new SnapshotQueries();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		for (var index = 0; index < 100; index++)
		{
			using var upload = MapTestData.Style(name: $"Style {index}");
			using var response = index == 0
				? await MapTestData.UploadFirstStyleAsync(client, game.Id, upload)
				: await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), upload);

			Assert.Equal(index + 1, (await MapTestData.ReadAsync(response)).Styles.Count);
		}

		capture.Contexts.Clear();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await using var viewer = new MapHubViewer(factory);

		await viewer.StartAsync(MapTestData.MapIdFor(game.Id), timeout.Token);
		await viewer.JoinAsync(MapTestData.MapIdFor(game.Id), timeout.Token);
		await viewer.JoinAsync(Guid.NewGuid(), timeout.Token);

		Assert.Empty(capture.Contexts);
		Assert.False(viewer.HasProcessingEvents);
		await viewer.UnwatchAsync(timeout.Token);
		await Task.Delay(1200, timeout.Token);
		Assert.Empty(capture.Contexts);
	}

	private sealed class SnapshotQueries : DbCommandInterceptor
	{
		public ConcurrentDictionary<Guid, int> Contexts { get; } = new();

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
			CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			if (command.CommandText.StartsWith("SELECT") && command.CommandText.Contains("\"Maps\""))
				Contexts.AddOrUpdate(eventData.Context!.ContextId.InstanceId, 1, (_, count) => count + 1);

			return ValueTask.FromResult(result);
		}
	}
}
