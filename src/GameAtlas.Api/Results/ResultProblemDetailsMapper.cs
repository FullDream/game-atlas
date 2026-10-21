using System.Diagnostics;
using GameAtlas.Domain.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace GameAtlas.Api.Results;

internal static class ResultProblemDetailsMapper
{
	internal static readonly ErrorType[] PriorityErrorTypes =
	[
		ErrorType.Unauthenticated,
		ErrorType.Forbidden,
		ErrorType.NotFound,
		ErrorType.Conflict,
		ErrorType.BusinessRule,
		ErrorType.Validation,
		ErrorType.Failure
	];

	internal static ProblemDetails CreateProblemDetails(HttpContext httpContext, params Error[] errors)
	{
		var errorType = PriorityErrorTypes.First(type => errors.Any(error => error.Type == type));
		var problemDetailsFactory = httpContext.RequestServices.GetService<ProblemDetailsFactory>();

		if (problemDetailsFactory is null)
			return CreateWithoutMvc(errorType, httpContext, errors);

		if (errorType == ErrorType.Validation)
			return problemDetailsFactory.CreateValidationProblemDetails(
				httpContext,
				BuildValidationErrors(errors),
				MapError(errorType));

		var problemDetails = problemDetailsFactory.CreateProblemDetails(
			httpContext,
			MapError(errorType),
			null,
			null,
			errors.FirstOrDefault()?.Message);

		if (errorType == ErrorType.Conflict)
			problemDetails.Extensions["errors"] = BuildErrorDictionary(errors);

		return problemDetails;
	}

	private static ProblemDetails CreateWithoutMvc(ErrorType errorType, HttpContext context, Error[] errors)
	{
		var status = MapError(errorType);

		// Match ASP.NET Core's existing defaults, including the historical RFC 4918 link for 422.
		var (title, type) = status switch
		{
			401 => ("Unauthorized", "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
			403 => ("Forbidden", "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
			404 => ("Not Found", "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
			409 => ("Conflict", "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
			422 => ("Unprocessable Entity", "https://tools.ietf.org/html/rfc4918#section-11.2"),
			_ => ("Bad Request", "https://tools.ietf.org/html/rfc9110#section-15.5.1")
		};
		ProblemDetails problem = errorType == ErrorType.Validation
			? new ValidationProblemDetails(BuildValidationErrors(errors))
			: new ProblemDetails { Title = title, Detail = errors.FirstOrDefault()?.Message };

		problem.Status = status;
		problem.Type = type;
		problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
		// MVC invokes this hook before adding the Conflict errors extension. Keep that order.
		context
			.RequestServices
			.GetService<IOptions<ProblemDetailsOptions>>()
			?.Value
			.CustomizeProblemDetails
			?.Invoke(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });

		if (errorType == ErrorType.Conflict)
			problem.Extensions["errors"] = BuildErrorDictionary(errors);

		return problem;
	}

	private static int MapError(ErrorType errorType) =>
		errorType switch
		{
			ErrorType.Unauthenticated => StatusCodes.Status401Unauthorized,
			ErrorType.Forbidden => StatusCodes.Status403Forbidden,
			ErrorType.NotFound => StatusCodes.Status404NotFound,
			ErrorType.BusinessRule or ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
			ErrorType.Conflict => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status400BadRequest
		};

	// Pure model construction; no MVC services are required. Reusing the existing
	// representation preserves key ordering, casing, and the validation error limit.
	private static Dictionary<string, string[]> BuildErrorDictionary(IEnumerable<Error> errors) =>
		BuildValidationErrors(errors)
			.ToDictionary(
				entry => entry.Key,
				entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

	private static ModelStateDictionary BuildValidationErrors(IEnumerable<Error> errors)
	{
		var stateDictionary = new ModelStateDictionary();

		foreach (var e in errors)
			stateDictionary.AddModelError(string.IsNullOrWhiteSpace(e.Property) ? string.Empty : e.Property, e.Message);

		return stateDictionary;
	}
}
