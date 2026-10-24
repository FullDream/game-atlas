using System.Data.Common;
using System.Net;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Infrastructure.Media.Storage;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameAtlas.IntegrationTests;

public sealed class MediaImmediateDeletionTests
{
	[Fact]
	public async Task StartupLeavesCrashTemporariesAloneAndRegistersNoMediaMaintenanceWorker()
	{
		await using var factory = new GameApiFactory();
		var path = Path.Combine(factory.MediaStoragePath, "staging", Guid.CreateVersion7().ToString("N"));

		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		await File.WriteAllTextAsync(path, "temporary file left by an interrupted process");
		await factory.MigrateAsync();

		Assert.True(File.Exists(path));
		Assert.DoesNotContain(factory.Services.GetServices<IHostedService>(), service =>
			service.GetType().Namespace?.StartsWith("GameAtlas.Infrastructure.Media", StringComparison.Ordinal) == true);
	}

	[Fact]
	public async Task PhysicalDeletionFailureRetainsMetadataAndASecondRequestDeletesImmediately()
	{
		var faults = new MapStorageFaults();
		await using var factory = new GameApiFactory(configureServices: services =>
		{
			services.AddSingleton<LocalMediaStorage>();
			services.AddSingleton<IMediaStorage>(provider =>
				new MediaFaultStorage(provider.GetRequiredService<LocalMediaStorage>(), faults));
		});

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client, "svg");
		var unrelated = await MediaTestData.UploadImageAsync(client, "svg");
		var path = Path.Combine(factory.MediaStoragePath, "assets", image.Id.ToString("N"));

		faults.FailCleanup = true;
		using var failed = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
		Assert.True(File.Exists(path));
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			Assert.True(await context.MediaAssets.AnyAsync(asset => asset.Id == image.Id));
		}

		faults.FailCleanup = false;
		using var deleted = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.False(File.Exists(path));
		Assert.True(File.Exists(Path.Combine(factory.MediaStoragePath, "assets", unrelated.Id.ToString("N"))));
	}

	[Fact]
	public async Task DatabaseFailureAfterPhysicalRemovalLeavesVisibleMissingFileMetadataWithoutRecoveryJobs()
	{
		await using var factory = new GameApiFactory(interceptor: new DeleteFault());

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client, "svg");
		using var failed = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
		Assert.False(File.Exists(Path.Combine(factory.MediaStoragePath, "assets", image.Id.ToString("N"))));
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.True(await context.MediaAssets.AnyAsync(asset => asset.Id == image.Id));
		using var original = await client.GetAsync(image.Url);

		Assert.Equal(HttpStatusCode.NotFound, original.StatusCode);
	}

	private sealed class DeleteFault : DbCommandInterceptor
	{
		public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
			CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			if (command.CommandText.StartsWith("DELETE FROM \"MediaAssets\"", StringComparison.Ordinal))
				throw new IOException("Injected database command failure after file deletion.");

			return ValueTask.FromResult(result);
		}
	}
}
