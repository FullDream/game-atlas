using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Application.Maps.Processing;
using GameAtlas.Application.Maps.Storage;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Application.Media.Inspection;
using GameAtlas.Infrastructure.Media.Inspection;
using GameAtlas.Infrastructure.Media.Storage;
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
		services.AddOptions<MediaStorageOptions>().Bind(configuration.GetSection("Media")).ValidateOnStart();
		services.AddOptions<MediaLimits>().Bind(configuration.GetSection("Media:Limits")).ValidateOnStart();
		services.AddSingleton<IValidateOptions<MediaStorageOptions>, MediaOptionsValidation>();
		services.AddSingleton<IValidateOptions<MediaLimits>, MediaOptionsValidation>();
		services.AddSingleton<IMediaStorage, LocalMediaStorage>();
		services.AddSingleton<RasterInspector>();
		services.AddSingleton<SvgInspector>();
		services.AddSingleton<VideoInspector>();
		services.AddSingleton<IMediaInspector, MediaInspector>();
		services.AddOptions<MapStorageOptions>().Bind(configuration.GetSection("Maps")).ValidateOnStart();
		services.AddSingleton<IValidateOptions<MapStorageOptions>, MapStorageOptionsValidation>();
		services.AddSingleton<IMapStorage, LocalMapStorage>();
		services.AddSingleton<IMapImageProcessor, VipsMapImageProcessor>();
		services.AddScoped<IMapCleanup, MapCleanup>();
		services.AddSingleton<MapProcessor>();
		services.AddSingleton<MapProgressStore>();
		services.AddSingleton<IMapProgress>(provider => provider.GetRequiredService<MapProgressStore>());
		services.AddSingleton<MapWorkQueue>();
		services.AddSingleton<IMapProcessingQueue>(provider => provider.GetRequiredService<MapWorkQueue>());
		services.AddSingleton<IMapMutationLock, MapMutationLock>();
		services.AddHostedService<MapProcessingService>();

		return services;
	}
}
