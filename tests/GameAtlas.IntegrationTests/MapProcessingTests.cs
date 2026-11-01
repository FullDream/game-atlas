using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Images;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapProcessingTests
{
	[Fact]
	public async Task HostedProcessorStartsOnlyAfterExplicitMigrationAndNeedsNoViewer()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);

		await MapTestData.ReadAsync(created);
		using var service = new MapProcessingService(factory.Services.GetRequiredService<MapProcessor>(),
			Microsoft.Extensions.Options.Options.Create(new Infrastructure.Maps.Storage.MapStorageOptions()));
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

		await service.StartAsync(timeout.Token);

		try
		{
			while (true)
			{
				await using var scope = factory.Services.CreateAsyncScope();
				var status = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().MapStyles
					.Select(source => source.Status).SingleAsync(timeout.Token);

				if (status == MapProcessingStatus.Ready)
					break;

				await Task.Delay(50, timeout.Token);
			}
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task FailedSiblingDoesNotBlockQueueAndRetryReusesOriginalAndSourceIdentity()
	{
		var images = new ControlledImages { FailuresRemaining = 1 };
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMapImageProcessor>(images));

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var initialUpload = MapTestData.Style();
		using var initial = await MapTestData.UploadFirstStyleAsync(client, game.Id, initialUpload);
		var map = await MapTestData.ReadAsync(initial);
		var atlas = map.Styles.Single();
		using var roadUpload = MapTestData.Style(name: "Roads");
		using var added = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), roadUpload);
		var appended = await MapTestData.ReadAsync(added);
		var roads = appended.Styles.Last();
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var processed = await MapTestData.ReadAsync(read);
		var failed = processed.Styles.First();

		Assert.Equal(MapProcessingStatus.Failed, failed.Status);
		Assert.Equal(77, failed.Progress);
		Assert.NotNull(failed.Error);
		Assert.DoesNotContain("private", failed.Error.Message);
		Assert.Equal(MapProcessingStatus.Ready, processed.Styles.Last().Status);
		Assert.Equal(roads.Id, processed.DefaultStyleId);
		using var retry = await client.PostAsync($"{MapTestData.PathFor(game.Id)}/styles/{atlas.Id}/retry", null);
		var requeued = await retry.Content.ReadFromJsonAsync<MapStyleDto>(MapTestData.JsonOptions);

		Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
		Assert.Equal(atlas.Id, requeued!.Id);
		Assert.Equal(0, requeued.Progress);
		Assert.Null(requeued.Error);
		await processor.CleanupAsync(CancellationToken.None);
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var readyRead = await client.GetAsync(MapTestData.PathFor(game.Id));
		var ready = await MapTestData.ReadAsync(readyRead);

		Assert.Equal(roads.Id, ready.DefaultStyleId);
		Assert.All(ready.Styles, style => Assert.Equal(MapProcessingStatus.Ready, style.Status));
		Assert.False(await processor.RunNextAsync(CancellationToken.None));
	}

	[Fact]
	public async Task WorkerSerializesNativeWorkWhileMetadataCanChangeAndDeletionCannotResurrectMap()
	{
		var images = new ControlledImages { Pause = true };
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMapImageProcessor>(images));

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var initialUpload = MapTestData.Style();
		using var initial = await MapTestData.UploadFirstStyleAsync(client, game.Id, initialUpload);
		var map = await MapTestData.ReadAsync(initial);
		var style = map.Styles.Single();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var processor = factory.Services.GetRequiredService<MapProcessor>();
		var first = processor.RunNextAsync(timeout.Token);

		await images.Started.Task.WaitAsync(timeout.Token);
		var second = processor.RunNextAsync(timeout.Token);
		using var patch = await client.PatchAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}",
			new { name = "While processing" }, timeout.Token);

		Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
		Assert.False(second.IsCompleted);
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id), timeout.Token);

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		images.Release.TrySetResult();
		Assert.True(await first);
		Assert.False(await second);
		Assert.Equal(1, images.MaximumConcurrency);
		await processor.CleanupAsync(timeout.Token);
		using var missing = await client.GetAsync(MapTestData.PathFor(game.Id), timeout.Token);

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		Assert.False(Directory.Exists(System.IO.Path.Combine(factory.MapStoragePath, "content", map.Id.ToString("N"))));
	}

	[Fact]
	public async Task InterruptedAttemptFailsAndRequiresManualRetry()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var initial = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(initial);
		var id = map.Styles.Single().Id;
		Guid oldAttempt;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var style = await context.MapStyles.AsTracking().SingleAsync();

			oldAttempt = style.AttemptId;
			Assert.True(style.Claim(oldAttempt).IsSuccess);
			await context.SaveChangesAsync();
		}

		var processor = factory.Services.GetRequiredService<MapProcessor>();

		await processor.RecoverAsync(CancellationToken.None);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var style = await context.MapStyles.SingleAsync();

			Assert.Equal(id, style.Id);
			Assert.Equal(MapProcessingStatus.Failed, style.Status);
			Assert.Equal(oldAttempt, style.AttemptId);
			Assert.True(style.Publish(oldAttempt).IsFailure);
		}

		await processor.CleanupAsync(CancellationToken.None);
		using var retry = await client.PostAsync($"{MapTestData.PathFor(game.Id)}/styles/{id}/retry", null);

		retry.EnsureSuccessStatusCode();
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var ready = await MapTestData.ReadAsync(read);

		Assert.Equal(MapProcessingStatus.Ready, ready.Styles.Single().Status);
	}

	internal sealed class ControlledImages : IMapImageProcessor
	{
		private readonly VipsMapImageProcessor inner = new();
		private int concurrency;

		public int FailuresRemaining { get; set; }
		public bool Pause { get; init; }
		public int MaximumConcurrency { get; private set; }
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void CheckCapabilities() => inner.CheckCapabilities();

		public async Task GenerateAsync(string path, string output, MapGeometry geometry, IProgress<int> progress,
			CancellationToken cancellationToken)
		{
			MaximumConcurrency = Math.Max(MaximumConcurrency, Interlocked.Increment(ref concurrency));

			try
			{
				progress.Report(77);
				Started.TrySetResult();

				if (Pause)
					await Release.Task.WaitAsync(cancellationToken);

				if (FailuresRemaining-- > 0)
					throw new IOException("private decoder failure");

				await inner.GenerateAsync(path, output, geometry, progress, cancellationToken);
			}
			finally
			{
				Interlocked.Decrement(ref concurrency);
			}
		}
	}
}
