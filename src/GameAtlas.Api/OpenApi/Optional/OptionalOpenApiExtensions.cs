using Microsoft.AspNetCore.OpenApi;

namespace GameAtlas.Api.OpenApi.Optional;

public static class OptionalOpenApiExtensions
{
	public static OpenApiOptions AddOptionalSupport(this OpenApiOptions options)
	{
		options.CreateSchemaReferenceId = jsonTypeInfo =>
		{
			if (OptionalSchemaTransformer.IsOptional(jsonTypeInfo.Type))
				return null;

			return OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
		};

		options.AddSchemaTransformer<OptionalSchemaTransformer>();

		return options;
	}
}
