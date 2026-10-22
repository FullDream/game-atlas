using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.OpenApi.Parameters;

internal sealed class CamelCaseQueryParametersTransformer : IOpenApiOperationTransformer
{
	public Task TransformAsync(
		OpenApiOperation operation,
		OpenApiOperationTransformerContext context,
		CancellationToken cancellationToken)
	{
		foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
		{
			if (parameter is { In: ParameterLocation.Query, Name: { } name })
				parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
		}

		return Task.CompletedTask;
	}
}
