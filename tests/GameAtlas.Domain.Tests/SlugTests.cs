using GameAtlas.Domain.ValueObjects;

namespace GameAtlas.Domain.Tests;

public class SlugTests
{
	[Theory]
	[InlineData("world")]
	[InlineData(" world-2 ")]
	[InlineData("123")]
	public void CreatesTrimmedValueWithOrdinalEquality(string input)
	{
		var slug = Slug.Create(input).Value!;

		string implicitValue = slug;

		Assert.Equal(input.Trim(), slug.Value);
		Assert.Equal(slug.Value, implicitValue);
		Assert.Equal(slug.Value, slug.ToString());
		Assert.Equal(slug, Slug.Create(input.Trim()).Value);
		Assert.Equal(slug.GetHashCode(), Slug.Create(input.Trim()).Value!.GetHashCode());
		Assert.NotEqual(slug, Slug.Create("other").Value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	[InlineData("Upper")]
	[InlineData("игра")]
	[InlineData("two words")]
	[InlineData("two--words")]
	[InlineData("-world")]
	[InlineData("world-")]
	[InlineData("world\nmap")]
	public void RejectsInvalidValues(string? value) => Assert.True(Slug.Create(value).IsFailure);

	[Fact]
	public void EnforcesTrimmedLengthBoundary()
	{
		Assert.True(Slug.Create(" " + new string('s', 200) + " ").IsSuccess);
		Assert.Equal(SlugErrors.TooLong, Assert.Single(Slug.Create(new string('s', 201)).Errors!));
	}
}
