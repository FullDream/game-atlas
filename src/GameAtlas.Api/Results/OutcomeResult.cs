using GameAtlas.Domain.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using DomainResult = GameAtlas.Domain.Result.Result;

namespace GameAtlas.Api.Results;

public class OutcomeResult(DomainResult result) : ActionResult, IResult
{
	protected DomainResult InnerResult { get; } = result;

	// Preserve existing formatter/customization behavior when the host opted into MVC.
	// A Minimal-only host requires no MVC services and uses native HTTP results.
	public Task ExecuteAsync(HttpContext httpContext) =>
		httpContext.RequestServices.GetService<IActionResultExecutor<ObjectResult>>() is not null
			? ExecuteResultAsync(new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor()))
			: ExecuteMinimalAsync(httpContext);

	protected virtual Task ExecuteMinimalAsync(HttpContext context) =>
		InnerResult.IsSuccess
			? TypedResults.NoContent().ExecuteAsync(context)
			: ExecuteMinimalFailure(context, InnerResult.Errors!);

	protected static Task ExecuteMinimalFailure(HttpContext context, Error[] errors)
	{
		var problem = ResultProblemDetailsMapper.CreateProblemDetails(context, errors);

		return TypedResults
			.Json(problem, statusCode: problem.Status, contentType: "application/problem+json; charset=utf-8")
			.ExecuteAsync(context);
	}

	public override Task ExecuteResultAsync(ActionContext context)
	{
		if (InnerResult.IsSuccess)
			return new NoContentResult().ExecuteResultAsync(context);

		return ExecuteFailure(context, InnerResult.Errors!);
	}

	protected static Task ExecuteFailure(ActionContext context, Error[] errors)
	{
		var problemDetails = ResultProblemDetailsMapper.CreateProblemDetails(context.HttpContext, errors);

		return new ObjectResult(problemDetails).ExecuteResultAsync(context);
	}

	public static implicit operator OutcomeResult(DomainResult result) => new(result);

	public static implicit operator OutcomeResult(Error error) => new(DomainResult.Failure(error));

	public static implicit operator OutcomeResult(Error[] errors) => new(DomainResult.Failure(errors));
}
