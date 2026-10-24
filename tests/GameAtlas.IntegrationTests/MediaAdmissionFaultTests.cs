using System.Data.Common;
using System.Net;
using GameAtlas.Application.Media.Inspection;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MediaAdmissionFaultTests
{
	[Fact]
	public async Task CancelledInspectionReleasesItsAdmissionAndStagedOriginal()
	{
		var inspector = new PausedInspection();
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IMediaInspector>(inspector));

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var cancellation = new CancellationTokenSource();
		using var body = new MultipartFormDataContent();

		body.Add(new StringContent("<svg xmlns='http://www.w3.org/2000/svg'/>"), "file", "icon.svg");
		var uploading = client.PostAsync("/media/assets", body, cancellation.Token);

		await inspector.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.Single(Directory.GetFiles(Path.Combine(factory.MediaStoragePath, "staging")));
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uploading);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		while (Directory.GetFiles(factory.MediaStoragePath, "*", SearchOption.AllDirectories).Length != 0)
			await Task.Delay(25, timeout.Token);

		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.False(await context.MediaAssets.AnyAsync(timeout.Token));
		Assert.Empty(Directory.GetFiles(factory.MediaStoragePath, "*", SearchOption.AllDirectories));
	}

	[Fact]
	public async Task ResponseLossAfterAssetCommitRetainsDiscoverableMetadataAndOriginal()
	{
		await using var factory = new GameApiFactory(interceptor: new CommitFault());

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var body = new MultipartFormDataContent();

		body.Add(new StringContent("<svg xmlns='http://www.w3.org/2000/svg'/>"), "file", "icon.svg");
		using var lost = await client.PostAsync("/media/assets", body);

		Assert.Equal(HttpStatusCode.InternalServerError, lost.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var asset = await context.MediaAssets.SingleAsync();
		using var original = await client.GetAsync($"/media/files/{asset.Id}");

		Assert.Equal(HttpStatusCode.OK, original.StatusCode);
		Assert.Empty(Directory.GetFiles(Path.Combine(factory.MediaStoragePath, "staging")));
	}

	private sealed class PausedInspection : IMediaInspector
	{
		internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public async Task<Result<MediaInspection>> InspectAsync(string path, CancellationToken cancellationToken)
		{
			Started.TrySetResult();
			await Task.Delay(Timeout.Infinite, cancellationToken);

			throw new InvalidOperationException("The test inspection must be cancelled.");
		}
	}

	private sealed class CommitFault : DbTransactionInterceptor
	{
		public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
			CancellationToken cancellationToken = default)
		{
			if (eventData.Context!.ChangeTracker.Entries<MediaAsset>().Any())
				throw new IOException("Injected response loss after the asset committed.");

			return Task.CompletedTask;
		}
	}
}
