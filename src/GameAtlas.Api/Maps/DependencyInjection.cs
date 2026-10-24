using GameAtlas.Api.Maps.Events;
using GameAtlas.Api.Maps.Uploads;

namespace GameAtlas.Api.Maps;

public static class DependencyInjection
{
	public static IServiceCollection AddMapsApi(this IServiceCollection services)
	{
		services.AddScoped<MapStyleMultipartReader>();
		services.AddScoped<MapUploadWorkflow>();
		services.AddSingleton<MapEventStream>();

		return services;
	}
}
