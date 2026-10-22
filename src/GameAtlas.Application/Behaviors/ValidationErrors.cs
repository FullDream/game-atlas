using FluentValidation.Results;
using GameAtlas.Domain.Result;

namespace GameAtlas.Application.Behaviors;

internal static class ValidationErrors
{
	internal static Error FromFailure(ValidationFailure failure) =>
		new(failure.ErrorCode, failure.ErrorMessage, failure.PropertyName, ErrorType.Validation);
}
