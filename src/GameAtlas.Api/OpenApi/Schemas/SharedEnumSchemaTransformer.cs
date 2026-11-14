using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.OpenApi.Schemas;

internal sealed class SharedEnumSchemaTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(
		OpenApiDocument document,
		OpenApiDocumentTransformerContext context,
		CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is not { } schemas)
			return Task.CompletedTask;

		var enumSchemas = FindUniqueEnumSchemas(schemas);

		if (enumSchemas.Count == 0)
			return Task.CompletedTask;

		foreach (var (id, schema) in schemas)
		{
			if (enumSchemas.ContainsValue(id) || schema is not OpenApiSchema mutable)
				continue;

			RewriteSchema(mutable, enumSchemas, document);
		}

		return Task.CompletedTask;
	}

	private static Dictionary<string, string> FindUniqueEnumSchemas(IDictionary<string, IOpenApiSchema> schemas)
	{
		var byValues = new Dictionary<string, string?>(StringComparer.Ordinal);

		foreach (var (id, schema) in schemas)
		{
			if (schema is not OpenApiSchema concrete || !IsSimpleStringEnum(concrete))
				continue;

			var key = CreateEnumKey(concrete.Enum!);

			byValues[key] = byValues.ContainsKey(key) ? null : id;
		}

		return byValues
			.Where(entry => entry.Value is not null)
			.ToDictionary(entry => entry.Key, entry => entry.Value!, StringComparer.Ordinal);
	}

	private static void RewriteSchema(
		OpenApiSchema schema,
		IReadOnlyDictionary<string, string> enumSchemas,
		OpenApiDocument document)
	{
		if (schema.Properties is { Count: > 0 })
		{
			foreach (var (name, property) in schema.Properties.ToArray())
				schema.Properties[name] = RewriteChild(property, enumSchemas, document);
		}

		if (schema.Items is not null)
			schema.Items = RewriteChild(schema.Items, enumSchemas, document);

		RewriteList(schema.AnyOf, enumSchemas, document);
		RewriteList(schema.OneOf, enumSchemas, document);
		RewriteList(schema.AllOf, enumSchemas, document);
	}

	private static void RewriteList(
		IList<IOpenApiSchema>? items,
		IReadOnlyDictionary<string, string> enumSchemas,
		OpenApiDocument document)
	{
		if (items is null)
			return;

		for (var index = 0; index < items.Count; index++)
			items[index] = RewriteChild(items[index], enumSchemas, document);
	}

	private static IOpenApiSchema RewriteChild(
		IOpenApiSchema child,
		IReadOnlyDictionary<string, string> enumSchemas,
		OpenApiDocument document)
	{
		if (child is not OpenApiSchema concrete)
			return child;

		if (IsSimpleStringEnum(concrete) && enumSchemas.TryGetValue(CreateEnumKey(concrete.Enum!), out var schemaId))
			return new OpenApiSchemaReference(schemaId, document);

		RewriteSchema(concrete, enumSchemas, document);

		return concrete;
	}

	private static bool IsSimpleStringEnum(OpenApiSchema schema) =>
		schema.Type is null or (JsonSchemaType)0 or JsonSchemaType.String &&
		schema.Enum is { Count: > 0 } &&
		schema.Enum.All(value => value?.GetValueKind() == System.Text.Json.JsonValueKind.String) &&
		schema.Description is null &&
		schema.Default is null &&
		!schema.ReadOnly &&
		!schema.WriteOnly &&
		!schema.Deprecated &&
		schema.Format is null &&
		schema.Pattern is null &&
		schema.MinLength is null &&
		schema.MaxLength is null &&
		schema.Properties is null or { Count: 0 } &&
		schema.Items is null &&
		schema.AllOf is null or { Count: 0 } &&
		schema.AnyOf is null or { Count: 0 } &&
		schema.OneOf is null or { Count: 0 };

	private static string CreateEnumKey(IList<JsonNode> values) =>
		string.Join('\0', values.Select(value => value.ToJsonString()));
}
