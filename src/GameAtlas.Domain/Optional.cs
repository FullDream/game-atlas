using System.Diagnostics.CodeAnalysis;

namespace GameAtlas.Domain;

public readonly struct Optional<T> : IOptional
{
	private readonly T innerValue;

	private Optional(T innerValue)
	{
		this.innerValue = innerValue;
		IsSpecified = true;
	}

	public bool IsSpecified { get; }

	public T Value =>
		IsSpecified ? innerValue : throw new InvalidOperationException("Optional value was not specified.");

	public static Optional<T> Unspecified => default;

	public static Optional<T> From(T value) => new(value);

	public bool TryGetValue([MaybeNullWhen(false)] out T value)
	{
		value = innerValue;

		return IsSpecified;
	}

	public static implicit operator Optional<T>(T value) => From(value);
}
