using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.Markers;

internal sealed class MarkerDocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
		CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is not { } schemas)
			return Task.CompletedTask;

		// Preserve native discriminator mappings by converting unions after schema promotion.
		foreach (var name in new[] { "MarkerMediaRequest", "MarkerMediaDto" })
		{
			if (!schemas.TryGetValue(name, out var value) || value is not OpenApiSchema { AnyOf: { Count: > 0 } } media)
				continue;

			media.OneOf = media.AnyOf;
			media.AnyOf = null;
		}

		return Task.CompletedTask;
	}
}
