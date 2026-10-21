using System.Diagnostics.CodeAnalysis;

namespace GameAtlas.Domain.Result;

public class Result : IFailureFactory<Result>
{
	protected Result(bool isSuccess, Error[]? errors)
	{
		IsSuccess = isSuccess;
		Errors = errors;
	}

	private Result() : this(true, null)
	{
	}

	private Result(params Error[] errors) : this(false, errors)
	{
	}

	[MemberNotNullWhen(true, nameof(Errors))]
	public bool IsFailure => !IsSuccess;

	[MemberNotNullWhen(false, nameof(Errors))]
	public bool IsSuccess { get; }

	public Error[]? Errors { get; } = null;

	public static Result Failure(params Error[] errors) => new(errors);

	public static Result Success() => new();

	public static implicit operator Result(Error error) => Failure(error);

	public static implicit operator Result(Error[] errors) => Failure(errors);
}

public class Result<T> : Result, IFailureFactory<Result<T>>
{
	private Result(T value) : base(true, null)
	{
		Value = value;
	}

	private Result(params Error[] errors) : base(false, errors)
	{
	}

	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Errors))]
	public new bool IsSuccess => base.IsSuccess;

	[MemberNotNullWhen(true, nameof(Errors))]
	[MemberNotNullWhen(false, nameof(Value))]
	public new bool IsFailure => base.IsFailure;

	public new Error[]? Errors => base.Errors;

	public T? Value { get; }

	public new static Result<T> Failure(params Error[] errors) => new(errors);

	public static Result<T> Success(T value) => new(value);

	public static implicit operator Result<T>(T value) => Success(value);
	public static implicit operator Result<T>(Error error) => Failure(error);
	public static implicit operator Result<T>(Error[] errors) => Failure(errors);
}
