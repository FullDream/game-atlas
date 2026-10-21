namespace GameAtlas.Api.OpenApi.Validation;

public static class OpenApiValidationExtensions
{
	public static IServiceCollection AddOpenApiValidationSource<TContract, TValidation>(
		this IServiceCollection services)
	{
		services.AddSingleton<IOpenApiValidationSource>(new OpenApiValidationSource<TContract, TValidation>());

		return services;
	}

	public static IServiceCollection AddOpenApiValidation(this IServiceCollection services)
	{
		services.AddSingleton<IOpenApiValidationSourceResolver, OpenApiValidationSourceResolver>();

		return services;
	}
}
