using FluentValidation;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
	: IPipelineBehavior<TMessage, TResponse>
	where TMessage : notnull, IMessage
	where TResponse : IFailureFactory<TResponse>
{
	public async ValueTask<TResponse> Handle(
		TMessage message,
		MessageHandlerDelegate<TMessage, TResponse> next,
		CancellationToken cancellationToken)
	{
		var errors = new List<Error>();

		cancellationToken.ThrowIfCancellationRequested();

		foreach (var validator in validators)
		{
			var result = await validator.ValidateAsync(message, cancellationToken);

			errors.AddRange(result.Errors.Select(ValidationErrors.FromFailure));
		}

		if (errors.Count > 0)
			return TResponse.Failure(errors.ToArray());

		return await next(message, cancellationToken);
	}
}
