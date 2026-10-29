using System.Data.Common;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Maps.AddMapStyle;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Application.Maps.Processing;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Maps.Storage;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapDispatchTests
{
	[Fact]
	public async Task QueueAdmissionSendsNothingAndWorkerNotifiesOnlyAfterReleasingMutationLock()
	{
		var notifications = new RecordingNotifications();
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMapNotifications>(notifications));
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "explicit-flow");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		var style = map.Styles.Single();
		var queue = factory.Services.GetRequiredService<IMapProcessingQueue>();

		notifications.VerifyUnlocked = async () =>
		{
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
			using var lease = await factory.Services.GetRequiredService<IMapMutationLock>().EnterAsync(timeout.Token);
		};

		queue.Enqueue(map.Id, style.Id, style.AttemptId);
		Assert.Empty(notifications.Styles);
		Assert.Empty(notifications.Progress);
		Assert.True(await factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(CancellationToken.None));
		Assert.Equal(new[] { MapProcessingStatus.Processing, MapProcessingStatus.Ready },
			notifications.Styles.Select(item => item.Status));
		Assert.All(notifications.Progress, item =>
		{
			Assert.Equal(MapProcessingStatus.Processing, item.Status);
			Assert.InRange(item.Progress, 1, 99);
		});
	}

	[Fact]
	public async Task PercentagesAndIdleQueueCleanupDoNotExecuteSql()
	{
		var sql = new SqlCounter();
		var images = new MapProcessingTests.ControlledImages { Pause = true };
		await using var factory = new GameApiFactory(interceptor: sql, configureServices: services =>
			services.AddSingleton<IMapImageProcessor>(images));
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "no-poll");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		await using var viewer = new MapHubViewer(factory);

		await viewer.StartAsync(map.Id, timeout.Token);
		var processor = factory.Services.GetRequiredService<MapProcessor>();
		var running = processor.RunNextAsync(timeout.Token);

		await images.Started.Task.WaitAsync(timeout.Token);
		var before = sql.Count;
		await Task.Delay(1300, timeout.Token);
		Assert.Equal(before, sql.Count);
		images.Release.TrySetResult();
		Assert.True(await running);
		before = sql.Count;

		for (var index = 0; index < 3; index++)
		{
			Assert.False(await processor.RunNextAsync(timeout.Token));
			await processor.CleanupAsync(timeout.Token);
		}

		Assert.Equal(before, sql.Count);
	}

	[Fact]
	public async Task StartupRestoresCommittedButUndispatchedQueuedWorkAndDispatchIsDeduplicated()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "restore");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.False(await processor.RunNextAsync(CancellationToken.None));
		await processor.RecoverAsync(CancellationToken.None);
		var queue = factory.Services.GetRequiredService<IMapProcessingQueue>();
		var style = map.Styles.Single();

		queue.Enqueue(map.Id, style.Id, style.AttemptId);
		queue.Enqueue(map.Id, style.Id, style.AttemptId);
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		Assert.False(await processor.RunNextAsync(CancellationToken.None));
	}

	[Fact]
	public async Task JoinBeforeHttpReadStillReceivesCommittedTerminalChanges()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "race");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await using var viewer = new MapHubViewer(factory);

		await viewer.StartAsync(map.Id, timeout.Token);
		var running = factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(timeout.Token);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id), timeout.Token);

		Assert.True(await running);
		var notification = await viewer.UntilStyleAsync(data => data.GetProperty("status").GetString() == "ready", timeout.Token);

		Assert.Equal(map.Styles.Single().Id, notification.GetProperty("id").GetGuid());
		Assert.Equal(100, notification.GetProperty("progress").GetInt32());
		using var current = await client.GetAsync(MapTestData.PathFor(game.Id), timeout.Token);

		Assert.Equal(MapProcessingStatus.Ready, (await MapTestData.ReadAsync(current)).Styles.Single().Status);
	}

	[Fact]
	public async Task PendingCleanupDelaysOnlyItsOwnRetryAndCannotEraseTheNextPyramid()
	{
		var faults = new MapStorageFaults();
		var images = new MapProcessingTests.ControlledImages { FailuresRemaining = 1 };
		await using var factory = new GameApiFactory(configureServices: services =>
		{
			services.AddSingleton<IMapImageProcessor>(images);
			services.AddSingleton<LocalMapStorage>();
			services.AddSingleton<IMapStorage>(provider =>
				new MapFaultStorage(provider.GetRequiredService<LocalMapStorage>(), faults));
		});

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "cleanup-retry");
		using var client = factory.CreateHttpsClient();
		using var firstUpload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, firstUpload);
		var map = await MapTestData.ReadAsync(created);
		var first = map.Styles.Single();
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		faults.CleanupStyleId = first.Id;
		faults.FailCleanup = true;
		await processor.CleanupAsync(CancellationToken.None);
		using var retried = await client.PostAsync($"{MapTestData.PathFor(game.Id)}/styles/{first.Id}/retry", null);
		using var siblingUpload = MapTestData.Style(name: "Roads");
		using var sibling = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), siblingUpload);

		retried.EnsureSuccessStatusCode();
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		Assert.False(await processor.RunNextAsync(CancellationToken.None));
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var styles = await context.MapStyles.OrderBy(style => style.CreationSequence).ToArrayAsync();

		Assert.Equal(MapProcessingStatus.Queued, styles[0].Status);
		Assert.Equal(MapProcessingStatus.Ready, styles[1].Status);
		faults.FailCleanup = false;
		await Task.Delay(TimeSpan.FromSeconds(6));
		await processor.CleanupAsync(CancellationToken.None);
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		await processor.CleanupAsync(CancellationToken.None);
		var storage = factory.Services.GetRequiredService<IMapStorage>();

		Assert.True(Directory.Exists(storage.TilesPath(map.Id, first.Id)));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DispatchRequiresCommitAndSurvivesRequestCancellationAfterCommit(bool cancelAfterCommit)
	{
		using var cancellation = new CancellationTokenSource();
		var fault = new AdmissionBoundary(cancellation, cancelAfterCommit);
		await using var factory = new GameApiFactory(interceptor: fault);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "commit-boundary");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var handler = new AddMapStyleHandler(context, factory.Services.GetRequiredService<IMapProgress>(),
			factory.Services.GetRequiredService<IMapMutationLock>(),
			factory.Services.GetRequiredService<IMapProcessingQueue>(),
			factory.Services.GetRequiredService<IMapNotifications>());
		var command = new AddMapStyle(game.Id, map.Id, "Other", "#fff", map.Styles.Single().SourceAssetId);

		fault.Armed = true;

		if (cancelAfterCommit)
		{
			var result = await handler.Handle(command, cancellation.Token);

			Assert.True(result.IsSuccess);
			Assert.True(cancellation.IsCancellationRequested);
		}
		else
		{
			await Assert.ThrowsAsync<IOException>(async () => await handler.Handle(command, cancellation.Token));
		}

		Assert.Equal(cancelAfterCommit,
			await factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(CancellationToken.None));
	}

	private sealed class RecordingNotifications : IMapNotifications
	{
		public List<MapStyleDto> Styles { get; } = [];
		public List<MapProcessingDto> Progress { get; } = [];
		public Func<Task>? VerifyUnlocked { get; set; }

		public async Task StyleUpdatedAsync(Guid mapId, MapStyleDto style)
		{
			if (VerifyUnlocked is { } verify)
				await verify();

			Styles.Add(style);
		}

		public Task ProcessingUpdatedAsync(MapProcessingDto processing)
		{
			Progress.Add(processing);

			return Task.CompletedTask;
		}
	}

	private sealed class AdmissionBoundary(CancellationTokenSource cancellation, bool cancelAfterCommit)
		: DbTransactionInterceptor
	{
		public bool Armed { get; set; }

		public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
			TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
		{
			if (Armed && !cancelAfterCommit)
			{
				Armed = false;

				throw new IOException("Injected admission rollback.");
			}

			return ValueTask.FromResult(result);
		}

		public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
			CancellationToken cancellationToken = default)
		{
			if (Armed && cancelAfterCommit)
			{
				Armed = false;
				cancellation.Cancel();
			}

			return Task.CompletedTask;
		}
	}

	private sealed class SqlCounter : DbCommandInterceptor
	{
		private int count;
		public int Count => Volatile.Read(ref count);

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
			CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref count);

			return ValueTask.FromResult(result);
		}

		public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
			CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref count);

			return ValueTask.FromResult(result);
		}
	}
}
