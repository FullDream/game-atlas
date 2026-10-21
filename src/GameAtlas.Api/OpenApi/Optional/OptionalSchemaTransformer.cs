using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using GameAtlas.Domain;

namespace GameAtlas.Api.OpenApi.Optional;

internal sealed class OptionalSchemaTransformer : IOpenApiSchemaTransformer
{
	public async Task TransformAsync(
		OpenApiSchema schema,
		OpenApiSchemaTransformerContext context,
		CancellationToken cancellationToken)
	{
		RemoveOptionalPropertiesFromRequired(schema, context);

		var type = context.JsonTypeInfo.Type;

		if (!IsOptional(type))
			return;

		var valueType = type.GetGenericArguments()[0];

		var valueSchema = await context.GetOrCreateSchemaAsync(
			valueType,
			context.ParameterDescription,
			cancellationToken);

		ReplaceSchema(schema, valueSchema);
	}

	private static void RemoveOptionalPropertiesFromRequired(
		OpenApiSchema schema,
		OpenApiSchemaTransformerContext context)
	{
		if (schema.Required is null)
			return;

		foreach (var property in context.JsonTypeInfo.Properties)
		{
			if (IsOptional(property.PropertyType))
				schema.Required.Remove(property.Name);
		}
	}

	internal static bool IsOptional(Type type) =>
		type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);

	private static void ReplaceSchema(OpenApiSchema target, OpenApiSchema source)
	{
		target.Type = source.Type;
		target.Format = source.Format;

		target.Items = source.Items;

		target.Properties = source.Properties;
		target.Required = source.Required;

		target.AdditionalProperties = source.AdditionalProperties;
		target.AdditionalPropertiesAllowed = source.AdditionalPropertiesAllowed;

		target.AllOf = source.AllOf;
		target.AnyOf = source.AnyOf;
		target.OneOf = source.OneOf;
		target.Not = source.Not;

		target.Enum = source.Enum;

		target.Minimum = source.Minimum;
		target.Maximum = source.Maximum;
		target.ExclusiveMinimum = source.ExclusiveMinimum;
		target.ExclusiveMaximum = source.ExclusiveMaximum;
		target.MultipleOf = source.MultipleOf;

		target.MinLength = source.MinLength;
		target.MaxLength = source.MaxLength;
		target.Pattern = source.Pattern;

		target.MinItems = source.MinItems;
		target.MaxItems = source.MaxItems;
		target.UniqueItems = source.UniqueItems;

		target.MinProperties = source.MinProperties;
		target.MaxProperties = source.MaxProperties;
	}
}
