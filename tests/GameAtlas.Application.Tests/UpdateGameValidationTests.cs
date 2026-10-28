using GameAtlas.Application.Games.UpdateGame;
using GameAtlas.Domain;
using GameAtlas.Domain.Games;

namespace GameAtlas.Application.Tests;

public class UpdateGameValidationTests
{
	private readonly UpdateGameValidation validator = new();

	[Fact]
	public async Task RejectsEmptyPatchButAcceptsOneSpecifiedMutableField()
	{
		var empty = await validator.ValidateAsync(new UpdateGame(Guid.NewGuid()));
		var name = await validator.ValidateAsync(new UpdateGame(Guid.NewGuid(), Name: "Game"));

		Assert.False(empty.IsValid);
		Assert.Equal(UpdateGameErrors.EmptyPatch.Code, Assert.Single(empty.Errors).ErrorCode);
		Assert.True(name.IsValid);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public async Task RejectsSpecifiedInvalidStringsWithOriginalFieldNames(string? value)
	{
		var input = Optional<string?>.From(value);
		var result = await validator.ValidateAsync(new UpdateGame(Guid.NewGuid(), input, input, Optional<Guid?>.From(null)));

		Assert.False(result.IsValid);
		Assert.Equal(new[] { "ImageAssetId", "Name", "Slug" },
			result.Errors.Select(error => error.PropertyName).Distinct().Order());
	}

	[Theory]
	[InlineData(0, true)]
	[InlineData(1, false)]
	public async Task ValidatesLengthsAfterTrimming(int excess, bool valid)
	{
		var result = await validator.ValidateAsync(new UpdateGame(Guid.NewGuid(),
			" " + new string('n', 200 + excess) + " ",
			" " + new string('s', 200 + excess) + " ",
			Guid.NewGuid()));

		Assert.Equal(valid, result.IsValid);

		if (!valid)
			Assert.Equal(new[] { "Name", "Slug" }, result.Errors.Select(error => error.PropertyName).Order());
	}

	[Theory]
	[InlineData("Upper")]
	[InlineData("bad--slug")]
	[InlineData("-game")]
	[InlineData("game-")]
	[InlineData("two words")]
	[InlineData("игра")]
	public async Task RejectsInvalidSlug(string slug)
	{
		var result = await validator.ValidateAsync(new UpdateGame(Guid.NewGuid(), Slug: slug));

		Assert.False(result.IsValid);
		Assert.Equal("Slug", Assert.Single(result.Errors).PropertyName);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(GameStatus.Draft, true)]
	[InlineData(GameStatus.Published, true)]
	[InlineData((GameStatus)99, false)]
	public async Task ValidatesSpecifiedStatus(GameStatus? status, bool valid)
	{
		var result = await validator.ValidateAsync(new UpdateGame(Guid.NewGuid(),
			Status: Optional<GameStatus?>.From(status)));

		Assert.Equal(valid, result.IsValid);
		Assert.All(result.Errors, error => Assert.Equal("Status", error.PropertyName));
	}
}
