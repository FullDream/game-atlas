using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.OpenApi.Parameters;

internal sealed class CamelCaseQueryEnumSchemaTransformer : IOpenApiSchemaTransformer
{
	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
		CancellationToken cancellationToken)
	{
		if (!(Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type).IsEnum ||
			schema.Enum is null)
			return Task.CompletedTask;

		for (var index = 0; index < schema.Enum.Count; index++)
		{
			if (schema.Enum[index] is JsonValue value && value.TryGetValue<string>(out var name))
				schema.Enum[index] = JsonValue.Create(JsonNamingPolicy.CamelCase.ConvertName(name));
		}

		return Task.CompletedTask;
	}
}
