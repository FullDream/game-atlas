namespace GameAtlas.Domain.Result;

public sealed record Error(string Code, string Message, string? Property = null, ErrorType Type = ErrorType.Failure);
