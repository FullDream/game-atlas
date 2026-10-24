using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Infrastructure.Maps.Images;
using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Maps.Storage;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("GameAtlas");

		if (string.IsNullOrWhiteSpace(connectionString))
			throw new InvalidOperationException("Connection string 'GameAtlas' is required.");

		services.AddDbContext<ApplicationDbContext>(options => options
			.UseSqlite(connectionString, sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
			.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

		services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
		services.AddSingleton<IPersistenceFailureClassifier, SqlitePersistenceFailureClassifier>();
		services.AddOptions<MapStorageOptions>().Bind(configuration.GetSection("Maps")).ValidateOnStart();
		services.AddOptions<MapUploadLimits>().Bind(configuration.GetSection("Maps:Limits")).ValidateOnStart();
		services.AddSingleton<IValidateOptions<MapStorageOptions>, MapStorageOptionsValidation>();
		services.AddSingleton<IValidateOptions<MapUploadLimits>, MapStorageOptionsValidation>();
		services.AddSingleton<IMapStorage, LocalMapStorage>();
		services.AddSingleton<IMapImageProcessor, VipsMapImageProcessor>();
		services.AddScoped<IMapAdmissionSessions, MapAdmissionSessions>();
		services.AddScoped<IMapCleanup, MapCleanup>();
		services.AddSingleton<MapProcessor>();
		services.AddHostedService<MapProcessingService>();

		return services;
	}
}
