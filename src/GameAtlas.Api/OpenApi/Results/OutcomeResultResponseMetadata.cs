using System.Collections;
using GameAtlas.Api.Results;
using Microsoft.AspNetCore.Mvc;

namespace GameAtlas.Api.OpenApi.Results;

internal sealed record OutcomeResultResponseMetadata(int StatusCode, Type Type, string? ContentType)
{
	// Keep the existing MVC schemas, even where the runtime mapper returns the
	// less specific ProblemDetails type. Changing these is a separate API change.
	private static readonly OutcomeResultResponseMetadata[] ErrorResponses =
	[
		new(400, typeof(ValidationProblemDetails), "application/problem+json"),
		new(401, typeof(ProblemDetails), "application/problem+json"),
		new(403, typeof(ProblemDetails), "application/problem+json"),
		new(404, typeof(ProblemDetails), "application/problem+json"),
		new(409, typeof(ValidationProblemDetails), "application/problem+json"),
		new(422, typeof(ValidationProblemDetails), "application/problem+json"),
		new(500, typeof(ProblemDetails), "application/problem+json")
	];

	internal static IReadOnlyList<OutcomeResultResponseMetadata> GetResponses(
		Type returnType,
		IEnumerable<string> httpMethods,
		bool hasRoutePlaceholders)
	{
		if (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) ||
										 returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
			returnType = returnType.GetGenericArguments()[0];

		var isGeneric = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(OutcomeResult<>);

		if (!isGeneric && returnType != typeof(OutcomeResult))
			return [];

		var payloadType = isGeneric ? returnType.GetGenericArguments()[0] : null;
		var responses = new List<OutcomeResultResponseMetadata>
		{
			payloadType is null ? new(204, typeof(void), null) : new(200, payloadType, "application/json")
		};
		var methods = httpMethods.ToHashSet(StringComparer.OrdinalIgnoreCase);

		foreach (var response in ErrorResponses)
		{
			if (methods.Contains("GET"))
			{
				if (response.StatusCode == 409 || (response.StatusCode == 404 && !hasRoutePlaceholders &&
												   payloadType is not null && IsCollection(payloadType)))
					continue;
			}
			else if (methods.Contains("POST") && !hasRoutePlaceholders)
			{
				if (response.StatusCode == 404)
					continue;
			}
			else if (methods.Contains("DELETE") && response.StatusCode is 422 or 409)
				continue;

			responses.Add(response);
		}

		return responses;
	}

	private static bool IsCollection(Type payload) =>
		payload.IsArray ||
		(payload.IsGenericType && (payload.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
								   payload.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))) ||
		typeof(IEnumerable).IsAssignableFrom(payload);
}
