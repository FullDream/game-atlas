using GameAtlas.Api.Maps.Events;

namespace GameAtlas.Api.Maps;

public static class DependencyInjection
{
	public static IServiceCollection AddMapsApi(this IServiceCollection services)
	{
		services.AddSingleton<MapEventStream>();

		return services;
	}
}
