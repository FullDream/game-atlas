using System.Net.Http.Json;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GameAtlas.IntegrationTests;

public sealed class GameApiFactory(string environment = "Production", IInterceptor? interceptor = null,
	Action<IServiceCollection>? configureServices = null, string? schema = null, bool migrateBeforeStartup = true)
	: WebApplicationFactory<Program>
{
	private const string BaseConnectionString = "Host=localhost;Port=5432;Database=game_atlas;Username=game_atlas;Password=game_atlas;Pooling=true;Maximum Pool Size=10";

	public string Schema { get; } = schema ?? $"test_{Guid.NewGuid():N}";
	public string ConnectionString => $"{BaseConnectionString};Search Path={Schema}";
	public string MapStoragePath { get; } = Path.Combine(Path.GetTempPath(), $"game-maps-{Guid.NewGuid():N}");
	public string MediaStoragePath { get; } = Path.Combine(Path.GetTempPath(), $"game-media-{Guid.NewGuid():N}");

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		using (var connection = new NpgsqlConnection(BaseConnectionString))
		{
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandText = $"CREATE SCHEMA IF NOT EXISTS {Schema}";
			command.ExecuteNonQuery();
		}

		var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options;

		if (migrateBeforeStartup)
		{
			using var db = new ApplicationDbContext(options);

			db.Database.Migrate();
		}

		builder.UseSetting("Admin:Login", "test-admin");
		builder.UseSetting("Admin:Password", "Test-only-password7!");
		builder.UseEnvironment(environment);
		builder.ConfigureLogging(logging => logging.ClearProviders());
		builder.UseSetting("ConnectionStrings:GameAtlas", ConnectionString);
		builder.UseSetting("Maps:WorkerEnabled", "false");
		builder.UseSetting("Maps:StorageRoot", MapStoragePath);
		builder.UseSetting("Media:StorageRoot", MediaStoragePath);
		builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
			new Dictionary<string, string?>
			{
				["ConnectionStrings:GameAtlas"] = ConnectionString,
				["Logging:LogLevel:Default"] = "None",
				["Maps:WorkerEnabled"] = "false",
				["Maps:StorageRoot"] = MapStoragePath,
				["Media:StorageRoot"] = MediaStoragePath,
			}));

		builder.ConfigureServices(services =>
		{
			services.AddDbContext<ApplicationDbContext>(options =>
			{
				options.UseNpgsql(ConnectionString);

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

	public HttpClient CreateGuestClient() => CreateClient(new WebApplicationFactoryClientOptions
	{
		BaseAddress = new Uri("https://localhost"),
		AllowAutoRedirect = false
	});

	internal string? SessionCookie { get; private set; }

	public async Task<HttpClient> CreateHttpsClientAsync()
	{
		var client = CreateGuestClient();

		await RefreshTokenAsync(client);
		using var login = await client.PostAsJsonAsync("/auth/login", new { login = "test-admin", password = "Test-only-password7!" });

		login.EnsureSuccessStatusCode();
		SessionCookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-GameAtlas.Session=")).Split(';')[0];
		await RefreshTokenAsync(client);

		return client;
	}

	public static async Task RefreshTokenAsync(HttpClient client)
	{
		using var response = await client.GetAsync("/auth/antiforgery");

		response.EnsureSuccessStatusCode();
		var token = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("XSRF-TOKEN="));

		client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
		client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token["XSRF-TOKEN=".Length..token.IndexOf(';')]);
	}

	public override async ValueTask DisposeAsync()
	{
		await base.DisposeAsync();

		await using (var schemaConnection = new NpgsqlConnection(ConnectionString))
		{
			NpgsqlConnection.ClearPool(schemaConnection);
		}

		if (schema is null)
		{
			await using var connection = new NpgsqlConnection(BaseConnectionString);
			await connection.OpenAsync();
			await using var command = connection.CreateCommand();
			command.CommandText = $"DROP SCHEMA IF EXISTS {Schema} CASCADE";
			await command.ExecuteNonQueryAsync();
		}

		if (Directory.Exists(MapStoragePath))
			Directory.Delete(MapStoragePath, true);

		if (Directory.Exists(MediaStoragePath))
			Directory.Delete(MediaStoragePath, true);
	}
}
