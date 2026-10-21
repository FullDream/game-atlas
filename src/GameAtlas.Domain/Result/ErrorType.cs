namespace GameAtlas.Domain.Result;

public enum ErrorType
{
	BusinessRule,
	Conflict,
	Failure,
	Forbidden,
	NotFound,
	Unauthenticated,
	Validation
}