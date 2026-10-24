using GameAtlas.Api.Media.Uploads;

namespace GameAtlas.Api.Media;

public static class DependencyInjection
{
	public static IServiceCollection AddMediaApi(this IServiceCollection services)
	{
		services.AddScoped<MediaUploadWorkflow>();

		return services;
	}
}
