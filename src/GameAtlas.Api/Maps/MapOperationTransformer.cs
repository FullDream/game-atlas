using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameAtlas.Api.Maps;

internal sealed class MapOperationTransformer : IOpenApiOperationTransformer
{
	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
		CancellationToken cancellationToken)
	{
		if (operation.OperationId == "StreamMapProgress")
		{
			operation.Responses!["200"] = new OpenApiResponse
			{
				Description = "Success",
				Content = new Dictionary<string, OpenApiMediaType>
				{
					["text/event-stream"] = new()
					{
						Schema = new OpenApiSchema
						{
							Type = JsonSchemaType.String
						}
					}
				}
			};

			operation.Description = "Native SSE: initial snapshot and changed progress events contain " +
				"{mapId,defaultStyleId,styles}. Idle heartbeat events contain {mapId} " +
				"about every 15 seconds. " +
				"mapDeleted contains {mapId} and closes; error contains {mapId,code,message} and closes. " +
				"Reconnect starts with current state without history replay; " +
				"disconnect does not cancel processing.";

			return Task.CompletedTask;
		}

		return Task.CompletedTask;
	}
}
