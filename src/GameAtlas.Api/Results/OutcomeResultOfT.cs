using GameAtlas.Domain.Result;
using Microsoft.AspNetCore.Mvc;

namespace GameAtlas.Api.Results;

public class OutcomeResult<T>(Result<T> result) : OutcomeResult(result)
{
	protected override Task ExecuteMinimalAsync(HttpContext context)
	{
		if (result.IsFailure)
			return ExecuteMinimalFailure(context, result.Errors!);

		// Match the default MVC null-output behavior.
		return result.Value is null
			? TypedResults.NoContent().ExecuteAsync(context)
			: TypedResults.Ok(result.Value).ExecuteAsync(context);
	}

	public override Task ExecuteResultAsync(ActionContext context)
	{
		if (result.IsSuccess)
			return new OkObjectResult(result.Value).ExecuteResultAsync(context);

		return ExecuteFailure(context, result.Errors!);
	}

	public static implicit operator OutcomeResult<T>(Result<T> result) => new(result);

	public static implicit operator OutcomeResult<T>(Error error) => new(Result<T>.Failure(error));

	public static implicit operator OutcomeResult<T>(Error[] errors) => new(Result<T>.Failure(errors));
}
