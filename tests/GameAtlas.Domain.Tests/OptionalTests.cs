using GameAtlas.Domain;

namespace GameAtlas.Domain.Tests;

public class OptionalTests
{
	[Fact]
	public void NoValuesAreSpecifiedForEmptyInputOrUnspecifiedValues()
	{
		Assert.False(Optional.AnySpecified());
		Assert.False(Optional.AnySpecified(Optional<string?>.Unspecified, Optional<Guid?>.Unspecified));
	}

	[Fact]
	public void ExplicitNullAndDefaultValuesCountAsSpecified()
	{
		Assert.True(Optional.AnySpecified(Optional<string?>.Unspecified, Optional<Guid?>.From(null)));
		Assert.True(Optional.AnySpecified(Optional<int>.From(0), Optional<string?>.Unspecified));
	}

	[Fact]
	public void UsesFallbackOnlyWhenUnspecified()
	{
		var unspecified = Optional<string?>.Unspecified;
		var specified = Optional<string?>.From("new");
		var cleared = Optional<string?>.From(null);

		Assert.Equal("current", unspecified.GetValueOrDefault("current"));
		Assert.Equal("new", specified.GetValueOrDefault("current"));
		Assert.Null(cleared.GetValueOrDefault("current"));
	}

	[Fact]
	public void PreservesSpecifiedDefaultValueAndNullableValue()
	{
		var unspecified = Optional<int>.Unspecified;
		var zero = Optional<int>.From(0);
		var nullable = Optional<int?>.From(null);

		Assert.Equal(42, unspecified.GetValueOrDefault(42));
		Assert.Equal(0, zero.GetValueOrDefault(42));
		Assert.Null(nullable.GetValueOrDefault(42));
	}
}
