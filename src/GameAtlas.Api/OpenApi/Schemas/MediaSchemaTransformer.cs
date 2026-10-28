using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.OpenApi.Schemas;

internal sealed class MediaSchemaTransformer : IOpenApiDocumentTransformer
{
	private static readonly string[] ConcreteNames = ["MediaFolder", "ImageAsset", "VideoAsset"];

	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
		CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is not { } schemas)
			return Task.CompletedTask;

		var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
		var candidates = new Dictionary<string, List<OpenApiSchema>>(StringComparer.Ordinal);

		foreach (var name in ConcreteNames)
		{
			foreach (var prefix in new[] { "", "MediaItem", "MediaAsset", name })
			{
				var identifier = prefix + name;

				if (!schemas.TryGetValue(identifier, out var schema))
					continue;

				aliases.Add(identifier, name);

				if (!candidates.TryGetValue(name, out var variants))
					candidates.Add(name, variants = []);

				variants.Add(GetConcreteSchema(schema, schemas, identifier));
			}
		}

		var walker = new OpenApiWalker(new SchemaReferenceVisitor(aliases));

		walker.Walk(document);

		foreach (var (name, variants) in candidates)
		{
			JsonNode? canonicalShape = null;

			foreach (var variant in variants)
			{
				walker.Walk(new OpenApiDocument
				{
					Components = new OpenApiComponents
					{
						Schemas = new Dictionary<string, IOpenApiSchema> { [name] = variant }
					}
				});

				var shape = SerializeSchema(variant);

				if (canonicalShape is not null && !JsonNode.DeepEquals(canonicalShape, shape))
					throw new InvalidOperationException($"Conflicting OpenAPI representations for {name}.");

				canonicalShape = shape;
			}

			schemas[name] = variants[0];
		}

		foreach (var (alias, name) in aliases)
		{
			if (alias != name)
				schemas.Remove(alias);
		}

		return Task.CompletedTask;
	}

	private static OpenApiSchema GetConcreteSchema(IOpenApiSchema schema,
		IDictionary<string, IOpenApiSchema> schemas, string identifier)
	{
		var required = new SortedSet<string>(schema.Required ?? Enumerable.Empty<string>(), StringComparer.Ordinal);

		if (schema.AnyOf is { Count: 1 } && schema.Discriminator is not null &&
			schema.AnyOf[0] is OpenApiSchemaReference { Reference.Id: { } childId })
			schema = schemas[childId];

		if (schema.Properties?.ContainsKey("type") != true || schema.AnyOf is { Count: > 0 } ||
			schema.Discriminator is not null)
			throw new InvalidOperationException($"Unexpected concrete media schema {identifier}.");

		required.UnionWith(schema.Required ?? Enumerable.Empty<string>());
		required.Add("type");

		var concrete = (OpenApiSchema)schema.CreateShallowCopy();

		concrete.Type = JsonSchemaType.Object;
		concrete.Required = required;

		return concrete;
	}

	private static JsonNode SerializeSchema(OpenApiSchema schema)
	{
		using var output = new StringWriter(CultureInfo.InvariantCulture);

		schema.SerializeAsV31(new OpenApiJsonWriter(output));

		return JsonNode.Parse(output.ToString())!;
	}

	private sealed class SchemaReferenceVisitor(IReadOnlyDictionary<string, string> aliases) : OpenApiVisitorBase
	{
		public override void Visit(OpenApiMediaType mediaType) => mediaType.Schema = Rewrite(mediaType.Schema);

		public override void Visit(IOpenApiParameter parameter)
		{
			if (parameter is OpenApiParameter mutable)
				mutable.Schema = Rewrite(mutable.Schema);
		}

		public override void Visit(IOpenApiHeader header)
		{
			if (header is OpenApiHeader mutable)
				mutable.Schema = Rewrite(mutable.Schema);
		}

		public override void Visit(IOpenApiSchema schema)
		{
			if (schema is not OpenApiSchema mutable)
				return;

			foreach (var (name, property) in mutable.Properties?.ToArray() ?? [])
				mutable.Properties![name] = Rewrite(property)!;

			RewriteList(mutable.AnyOf);
			RewriteList(mutable.OneOf);
			RewriteList(mutable.AllOf);

			mutable.Items = Rewrite(mutable.Items);
			mutable.AdditionalProperties = Rewrite(mutable.AdditionalProperties);
			mutable.Not = Rewrite(mutable.Not);

			foreach (var (value, mapping) in mutable.Discriminator?.Mapping?.ToArray() ?? [])
				mutable.Discriminator!.Mapping![value] = (OpenApiSchemaReference)Rewrite(mapping)!;
		}

		private void RewriteList(IList<IOpenApiSchema>? schemas)
		{
			if (schemas is null)
				return;

			for (var index = 0; index < schemas.Count; index++)
				schemas[index] = Rewrite(schemas[index])!;
		}

		private IOpenApiSchema? Rewrite(IOpenApiSchema? schema)
		{
			if (schema is not OpenApiSchemaReference { Reference: { IsExternal: false, Id: { } id } } reference ||
				!aliases.TryGetValue(id, out var name))
				return schema;

			return new OpenApiSchemaReference(name)
			{
				Reference = new JsonSchemaReference(reference.Reference) { Id = name }
			};
		}
	}
}
