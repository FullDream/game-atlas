using GameAtlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class GameApiFactory(string environment = "Production", IInterceptor? interceptor = null)
	: WebApplicationFactory<Program>
{
	public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"game-api-{Guid.NewGuid():N}.db");

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment(environment);
		builder.UseSetting("ConnectionStrings:GameAtlas", $"Data Source={DatabasePath};Pooling=False");
		builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
			new Dictionary<string, string?>
			{
				["ConnectionStrings:GameAtlas"] = $"Data Source={DatabasePath};Pooling=False",
				["Logging:LogLevel:Default"] = "None"
			}));
		builder.ConfigureServices(services =>
		{
			services.AddDbContext<ApplicationDbContext>(options =>
			{
				options.UseSqlite($"Data Source={DatabasePath};Pooling=False");

				if (interceptor is not null)
					options.AddInterceptors(interceptor);
			});
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
	}
}
