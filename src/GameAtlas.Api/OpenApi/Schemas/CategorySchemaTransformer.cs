using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.OpenApi.Schemas;

internal sealed class CategorySchemaTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is not { } schemas)
			return Task.CompletedTask;

		foreach (var name in new[] { "Category", "CategoryGroup" })
		{
			if (!schemas.TryGetValue(name, out var resource) || resource is not OpenApiSchema { Properties: { } properties })
				continue;

			foreach (var field in new[] { "id", "mapId", "markersCount", "createdAt", "updatedAt", "categories", "icon" })
			{
				if (properties.TryGetValue(field, out var property) && property is OpenApiSchema concrete)
					concrete.ReadOnly = true;
			}
		}

		return Task.CompletedTask;
	}
}
