using GameAtlas.Infrastructure.Maps.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Maps.Processing;

internal sealed class MapProcessingService(MapProcessor processor, IOptions<MapStorageOptions> options)
	: BackgroundService
{
	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		if (options.Value.WorkerEnabled)
		{
			await processor.RecoverAsync(cancellationToken);
			await processor.CleanupAsync(cancellationToken);
		}

		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!options.Value.WorkerEnabled)
			return;

		while (!stoppingToken.IsCancellationRequested)
		{
			if (!await processor.RunNextAsync(stoppingToken))
				await processor.WaitAsync(stoppingToken);
		}
	}
}
