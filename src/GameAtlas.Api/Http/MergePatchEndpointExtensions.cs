using Microsoft.Net.Http.Headers;

namespace GameAtlas.Api.Http;

internal static class MergePatchEndpointExtensions
{
	internal static RouteHandlerBuilder WithMergePatch<TRequest>(this RouteHandlerBuilder builder)
		where TRequest : notnull
	{
		builder.Accepts<TRequest>("application/merge-patch+json", "application/json")
			.ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

		IEndpointConventionBuilder conventions = builder;

		conventions.Finally(ApplyContentTypeValidation);

		return builder;
	}

	private static void ApplyContentTypeValidation(EndpointBuilder endpoint)
	{
		var next = endpoint.RequestDelegate ??
			throw new InvalidOperationException("The endpoint request delegate must be initialized.");

		endpoint.RequestDelegate = context => IsSupportedContentType(context.Request.ContentType)
			? next(context)
			: TypedResults.Problem(statusCode: StatusCodes.Status415UnsupportedMediaType).ExecuteAsync(context);
	}

	private static bool IsSupportedContentType(string? contentType)
	{
		if (!MediaTypeHeaderValue.TryParse(contentType, out var parsedContentType))
			return false;

		var mediaType = parsedContentType.MediaType.Value;

		return string.Equals(mediaType, "application/merge-patch+json", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
	}
}
