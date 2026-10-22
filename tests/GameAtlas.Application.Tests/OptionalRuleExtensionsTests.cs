using FluentValidation;
using GameAtlas.Application.Common.Validation;
using GameAtlas.Domain;

namespace GameAtlas.Application.Tests;

public class OptionalRuleExtensionsTests
{
	[Fact]
	public async Task SkipsUnspecifiedValueWithoutReadingIt()
	{
		var validator = new InlineValidator<Request>();
		var called = false;

		validator.OptionalRuleFor(request => request.Name, rule => rule.Must(_ =>
		{
			called = true;

			return false;
		}));

		var result = await validator.ValidateAsync(new Request());

		Assert.True(result.IsValid);
		Assert.False(called);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("Game", true)]
	public async Task ValidatesSpecifiedValueWithOriginalPropertyName(string? value, bool valid)
	{
		var validator = new InlineValidator<Request>();

		validator.OptionalRuleFor(request => request.Name, rule => rule.NotEmpty().MaximumLength(20));
		var result = await validator.ValidateAsync(new Request { Name = Optional<string?>.From(value) });

		Assert.Equal(valid, result.IsValid);
		Assert.All(result.Errors, error => Assert.Equal("Name", error.PropertyName));
	}

	[Fact]
	public async Task SupportsAsynchronousRules()
	{
		var validator = new InlineValidator<Request>();
		using var source = new CancellationTokenSource();
		var called = false;

		validator.OptionalRuleFor(request => request.Name, rule => rule.MustAsync((_, token) =>
		{
			called = true;
			Assert.Equal(source.Token, token);

			return Task.FromResult(false);
		}));

		var result = await validator.ValidateAsync(new Request { Name = "Game" }, source.Token);

		Assert.True(called);
		Assert.False(result.IsValid);
	}

	[Fact]
	public async Task SkipsUnspecifiedCollection()
	{
		var validator = new InlineValidator<Request>();

		validator.OptionalRuleForEach(request => request.Names, rule => rule.NotEmpty());
		var result = await validator.ValidateAsync(new Request());

		Assert.True(result.IsValid);
	}

	[Fact]
	public async Task ValidatesCollectionItemsWithOriginalNameAndIndex()
	{
		var validator = new InlineValidator<Request>();

		validator.OptionalRuleForEach(request => request.Names, rule => rule.NotEmpty());
		var result = await validator.ValidateAsync(new Request
		{
			Names = Optional<IReadOnlyCollection<string>>.From(["Game", ""])
		});

		Assert.False(result.IsValid);
		Assert.Equal("Names[1]", Assert.Single(result.Errors).PropertyName);
	}

	[Fact]
	public void RejectsNonMemberSelectorsAsConfigurationErrors()
	{
		var validator = new InlineValidator<Request>();

		Assert.Throws<ArgumentException>(() => validator.OptionalRuleFor(
			request => GetName(request), rule => rule.NotEmpty()));
		Assert.Throws<ArgumentException>(() => validator.OptionalRuleForEach(
			request => GetNames(request), rule => rule.NotEmpty()));
	}

	private static Optional<string?> GetName(Request request) => request.Name;

	private static Optional<IReadOnlyCollection<string>> GetNames(Request request) => request.Names;

	private sealed class Request
	{
		public Optional<string?> Name { get; init; }
		public Optional<IReadOnlyCollection<string>> Names { get; init; }
	}
}
