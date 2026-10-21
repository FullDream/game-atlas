using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace GameAtlas.Api.OpenApi.Results;

internal static class OutcomeResultMetadataConvention
{
	internal static void Apply(EndpointBuilder builder)
	{
		// MVC owns its metadata through IApplicationModelConvention.
		if (builder is not RouteEndpointBuilder route || builder.Metadata.OfType<ControllerActionDescriptor>().Any())
			return;

		var method = builder.Metadata.OfType<MethodInfo>().LastOrDefault();

		if (method is null)
			return;

		var methods = builder.Metadata.OfType<IHttpMethodMetadata>().LastOrDefault()?.HttpMethods ?? [];
		var responses = OutcomeResultResponseMetadata.GetResponses(
			method.ReturnType,
			methods,
			route.RoutePattern.Parameters.Count > 0);
		var explicitStatuses = builder
			.Metadata
			.Select(metadata => metadata switch
			{
				IProducesResponseTypeMetadata produces => produces.StatusCode,
				IApiResponseMetadataProvider produces => produces.StatusCode,
				_ => (int?)null
			})
			.OfType<int>()
			.ToHashSet();

		foreach (var response in responses)
		{
			if (explicitStatuses.Add(response.StatusCode))
				builder.Metadata.Add(new ResponseMetadata(response));
		}
	}

	private sealed class ResponseMetadata(OutcomeResultResponseMetadata response) : IProducesResponseTypeMetadata
	{
		public int StatusCode => response.StatusCode;
		public Type Type => response.Type;
		public IEnumerable<string> ContentTypes => response.ContentType is null ? [] : [response.ContentType];
	}
}
