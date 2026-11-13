using System.Text.RegularExpressions;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.ValueObjects;

public sealed record Slug
{
	public const int MaxLength = 200;
	public const string Pattern = @"\A[a-z0-9]+(-[a-z0-9]+)*\z";
	private static readonly Regex syntax = new(Pattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

	private Slug(string value) => Value = value;

	public string Value { get; }

	public static Result<Slug> Create(string? value)
	{
		value = value?.Trim();

		if (string.IsNullOrEmpty(value))
			return SlugErrors.Required;

		if (value.Length > MaxLength)
			return SlugErrors.TooLong;

		if (!syntax.IsMatch(value))
			return SlugErrors.InvalidFormat;

		return new Slug(value);
	}

	public override string ToString() => Value;

	public static implicit operator string(Slug slug) => slug.Value;
}
