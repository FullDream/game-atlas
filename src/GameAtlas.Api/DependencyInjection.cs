using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using GameAtlas.Api.OpenApi.Optional;
using GameAtlas.Api.OpenApi.Validation;
using GameAtlas.Api.Serialization;
using GameAtlas.Api.Serialization.Optional;

namespace GameAtlas.Api;

public static class DependencyInjection
{
	public static IServiceCollection AddApi(this IServiceCollection services)
	{
		services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.AddOptionalSupport();
			options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
			options.SerializerOptions.Converters.Add(
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
			options.SerializerOptions.DictionaryKeyPolicy = new SegmentedNamingPolicy(JsonNamingPolicy.CamelCase);
			options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
		});
		services.AddValidatorsFromAssemblyContaining<FluentValidationSchemaTransformer>(includeInternalTypes: true);
		services.AddOpenApiValidation();
		services.AddOpenApi(options =>
		{
			options.AddOptionalSupport();
			options.AddSchemaTransformer<FluentValidationSchemaTransformer>();
		});

		return services;
	}
}
