using GameAtlas.Infrastructure.Maps.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Maps.Processing;

internal sealed class MapProcessingService(MapProcessor processor, IOptions<MapStorageOptions> options)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!options.Value.WorkerEnabled)
			return;

		await processor.RecoverAsync(stoppingToken);
		var reconciliationAt = DateTimeOffset.UtcNow.AddSeconds(30);

		while (!stoppingToken.IsCancellationRequested)
		{
			if (DateTimeOffset.UtcNow >= reconciliationAt)
			{
				await processor.RecoverAsync(stoppingToken);
				reconciliationAt = DateTimeOffset.UtcNow.AddSeconds(30);
			}

			await processor.RunNextAsync(stoppingToken);
			await processor.CleanupAsync(stoppingToken);
			await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
		}
	}
}
