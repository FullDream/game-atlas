using System.Diagnostics.CodeAnalysis;

namespace GameAtlas.Domain;

public static class Optional
{
	public static bool AnySpecified(params ReadOnlySpan<IOptional> values)
	{
		foreach (var value in values)
		{
			if (value.IsSpecified)
				return true;
		}

		return false;
	}
}

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

	public T GetValueOrDefault(T defaultValue) => IsSpecified ? innerValue : defaultValue;

	public bool TryGetValue([MaybeNullWhen(false)] out T value)
	{
		value = innerValue;

		return IsSpecified;
	}

	public static implicit operator Optional<T>(T value) => From(value);
}
