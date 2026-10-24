using GameAtlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameAtlas.IntegrationTests;

public sealed class GameApiFactory(string environment = "Production", IInterceptor? interceptor = null,
	Action<IServiceCollection>? configureServices = null)
	: WebApplicationFactory<Program>
{
	public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"game-api-{Guid.NewGuid():N}.db");
	public string MapStoragePath { get; } = Path.Combine(Path.GetTempPath(), $"game-maps-{Guid.NewGuid():N}");
	public string MediaStoragePath { get; } = Path.Combine(Path.GetTempPath(), $"game-media-{Guid.NewGuid():N}");

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment(environment);
		builder.ConfigureLogging(logging => logging.ClearProviders());
		builder.UseSetting("ConnectionStrings:GameAtlas", $"Data Source={DatabasePath};Pooling=False");
		builder.UseSetting("Maps:WorkerEnabled", "false");
		builder.UseSetting("Maps:StorageRoot", MapStoragePath);
		builder.UseSetting("Media:StorageRoot", MediaStoragePath);
		builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
			new Dictionary<string, string?>
			{
				["ConnectionStrings:GameAtlas"] = $"Data Source={DatabasePath};Pooling=False",
				["Logging:LogLevel:Default"] = "None",
				["Maps:WorkerEnabled"] = "false",
				["Maps:StorageRoot"] = MapStoragePath,
				["Media:StorageRoot"] = MediaStoragePath,
			}));
		builder.ConfigureServices(services =>
		{
			services.AddDbContext<ApplicationDbContext>(options =>
			{
				options.UseSqlite($"Data Source={DatabasePath};Pooling=False");

				if (interceptor is not null)
					options.AddInterceptors(interceptor);
			});

			configureServices?.Invoke(services);
		});
	}

	public async Task MigrateAsync()
	{
		await using var scope = Services.CreateAsyncScope();

		await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
	}

	public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
	{
		BaseAddress = new Uri("https://localhost"),
		AllowAutoRedirect = false
	});

	public override async ValueTask DisposeAsync()
	{
		await base.DisposeAsync();
		File.Delete(DatabasePath);
		File.Delete(DatabasePath + "-wal");
		File.Delete(DatabasePath + "-shm");

		if (Directory.Exists(MapStoragePath))
			Directory.Delete(MapStoragePath, true);

		if (Directory.Exists(MediaStoragePath))
			Directory.Delete(MediaStoragePath, true);
	}
}
