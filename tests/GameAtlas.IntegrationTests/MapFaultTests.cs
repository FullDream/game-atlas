using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Infrastructure.Media.Storage;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Maps.Storage;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapFaultTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FailedReadySaveAndDeletionPreserveOriginalsAndOwnedContent(bool deleteMap)
	{
		var fault = new SaveFault();
		await using var factory = Factory(new MapStorageFaults(), fault);

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Resources", 1);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonDocument.Parse(MarkerTestData.MixedMedia).RootElement.Clone());
		using var siblingCreated = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = "Dungeon", width = 512, height = 512 });
		var sibling = await MapTestData.ReadAsync(siblingCreated);

		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = map.Styles.Single();
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		fault.FailReady = true;
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var failed = (await MapTestData.ReadAsync(read)).Styles.Single();

		Assert.Equal(MapProcessingStatus.Failed, failed.Status);
		Assert.Null(failed.TilesUrlTemplate);
		Assert.InRange(failed.Progress, 0, 99);
		fault.FailCleanupSave = true;
		using var rejectedDelete = await client.DeleteAsync(deleteMap
			? MapTestData.PathFor(game.Id, map.Id)
			: $"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.InternalServerError, rejectedDelete.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.True(await context.Games.AnyAsync(game => game.Id == map.GameId));
		Assert.True(await context.Maps.AnyAsync(current => current.Id == map.Id));
		Assert.True(await context.Maps.AnyAsync(current => current.Id == sibling.Id));
		Assert.Equal(group.Id, (await context.CategoryGroups.SingleAsync()).Id);
		Assert.Equal(category.Id, (await context.Categories.SingleAsync()).Id);
		var storedMarker = await context.Markers.Include(item => item.Media).SingleAsync();

		Assert.Equal(marker.GetProperty("id").GetGuid(), storedMarker.Id);
		Assert.Equal(3, storedMarker.Media.Count);
		Assert.False(await context.Set<MapCleanupIntent>()
			.AnyAsync(intent => intent.MapId == map.Id && !intent.OutputsOnly));
		var storage = factory.Services.GetRequiredService<IMapStorage>();

		Assert.True(File.Exists(factory.Services.GetRequiredService<IMediaStorage>().AssetPath(style.Source.Id)));
	}

	private sealed class SaveFault : SaveChangesInterceptor
	{
		public bool FailReady { get; set; }
		public bool FailCleanupSave { get; set; }

		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
			InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			var context = eventData.Context!;
			var publishing = context.ChangeTracker.Entries<MapStyle>().Any(entry =>
				entry.State == EntityState.Modified && entry.Entity.Status == MapProcessingStatus.Ready);
			var deleting = context.ChangeTracker.Entries<MapCleanupIntent>().Any(entry =>
				entry.State == EntityState.Added && entry.Entity.MapId.HasValue && !entry.Entity.OutputsOnly);

			if (FailReady && publishing || FailCleanupSave && deleting)
				throw new IOException("Injected pre-commit storage failure.");

			return ValueTask.FromResult(result);
		}
	}

	private static GameApiFactory Factory(MapStorageFaults faults, IInterceptor? interceptor = null) =>
		new(interceptor: interceptor, configureServices: services =>
		{
			services.AddSingleton<LocalMapStorage>();
			services.AddSingleton<LocalMediaStorage>();
			services.AddSingleton<IMediaStorage>(provider =>
				new MediaFaultStorage(provider.GetRequiredService<LocalMediaStorage>(), faults));

			services.AddSingleton<IMapStorage>(provider => new MapFaultStorage(
				provider.GetRequiredService<LocalMapStorage>(), faults));
		});

	[Fact]
	public async Task FailurePromotingOriginalRollsBackAllRowsAndOnlyItsOwnFiles()
	{
		var faults = new MapStorageFaults { FailPromotionNumber = 1 };
		await using var factory = Factory(faults);

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var content = MapTestData.Style();
		using var response = await MapTestData.UploadFirstStyleAsync(client, game.Id, content);

		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		Assert.Empty(Directory.EnumerateFiles(factory.MapStoragePath, "*", SearchOption.AllDirectories));
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.True(await context.Maps.AnyAsync());
		Assert.False(await context.MapStyles.AnyAsync());
	}

	[Fact]
	public async Task FailureAfterAdmissionCommitLeavesDiscoverableDurableWork()
	{
		var fault = new AfterCommitFault();
		await using var factory = Factory(new MapStorageFaults(), fault);

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();

		await MapTestData.EnsureMapAsync(factory, game.Id);
		var image = await MediaTestData.UploadImageAsync(client, width: 23, height: 17);

		fault.Armed = true;
		using var lost = await client.PostAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles",
			new { name = "Atlas", backgroundColor = "#fff", sourceAssetId = image.Id });

		Assert.Equal(HttpStatusCode.InternalServerError, lost.StatusCode);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var admitted = await MapTestData.ReadAsync(read);
		var style = admitted.Styles.Single();
		var storage = factory.Services.GetRequiredService<IMapStorage>();

		Assert.True(File.Exists(factory.Services.GetRequiredService<IMediaStorage>().AssetPath(style.Source.Id)));
		Assert.True(await factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(CancellationToken.None));
	}

	[Fact]
	public async Task CompletePyramidIsPublicBeforeReadyCommitAndRecoveryRebuildsIt()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var dto = map.Styles.Single();
		Guid attempt;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var style = await context.MapStyles.AsTracking().SingleAsync();

			attempt = style.AttemptId;
			Assert.True(style.Claim(attempt).IsSuccess);
			await context.SaveChangesAsync();
		}

		var storage = factory.Services.GetRequiredService<IMapStorage>();
		var images = factory.Services.GetRequiredService<IMapImageProcessor>();

		await images.GenerateAsync(factory.Services.GetRequiredService<IMediaStorage>().AssetPath(dto.Source.Id),
			storage.WorkPath(map.Id, dto.Id, attempt),
			MapGeometry.Create(map.Width, map.Height).Value!,
			new Progress<int>(), CancellationToken.None);
		storage.Publish(map.Id, dto.Id, attempt);
		using var unpublished = await client.GetAsync(
			$"{MapTestData.TilesPath(map.Id, dto.Id)}/0/0/0.webp");

		Assert.Equal(HttpStatusCode.OK, unpublished.StatusCode);
		using var pending = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Null((await MapTestData.ReadAsync(pending)).Styles.Single().TilesUrlTemplate);
		var restarted = ActivatorUtilities.CreateInstance<MapProcessor>(factory.Services);

		await restarted.RecoverAsync(CancellationToken.None);
		Assert.True(await restarted.RunNextAsync(CancellationToken.None));
		using var ready = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(MapProcessingStatus.Ready, (await MapTestData.ReadAsync(ready)).Styles.Single().Status);
	}

	[Fact]
	public async Task FailedPublicationCleansOnlyOutputAndDurableCleanupRetriesAfterReopeningDatabase()
	{
		var faults = new MapStorageFaults { FailAfterPublicationMove = true };
		await using var factory = Factory(faults);

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = map.Styles.Single();
		var processor = factory.Services.GetRequiredService<MapProcessor>();
		var storage = factory.Services.GetRequiredService<IMapStorage>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		await processor.CleanupAsync(CancellationToken.None);
		Assert.True(File.Exists(factory.Services.GetRequiredService<IMediaStorage>().AssetPath(style.Source.Id)));
		Assert.False(Directory.Exists(storage.TilesPath(map.Id, style.Id)));
		faults.FailCleanup = true;
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		await processor.CleanupAsync(CancellationToken.None);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var intent = await context.Set<MapCleanupIntent>().SingleAsync(intent => intent.MapId == map.Id);

			Assert.True(intent.Attempts > 0);
			await context.Set<MapCleanupIntent>().Where(intent => intent.MapId == map.Id)
				.ExecuteUpdateAsync(setters => setters.SetProperty(intent => intent.RetryAt,
					DateTimeOffset.UtcNow.AddSeconds(-1)));
		}

		faults.FailCleanup = false;
		var restarted = ActivatorUtilities.CreateInstance<MapProcessor>(factory.Services);

		await restarted.CleanupAsync(CancellationToken.None);
		Assert.True(File.Exists(factory.Services.GetRequiredService<IMediaStorage>().AssetPath(style.Source.Id)));
	}

	private sealed class AfterCommitFault : DbTransactionInterceptor
	{
		public bool Armed { get; set; }

		public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
			CancellationToken cancellationToken = default)
		{
			if (Armed && eventData.Context!.ChangeTracker.Entries<Map>().Any())
			{
				Armed = false;

				throw new IOException("Injected response loss after admission commit.");
			}

			return Task.CompletedTask;
		}
	}
}
