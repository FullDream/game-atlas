using GameAtlas.Domain.ValueObjects;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Tests;

public class GameTests
{
	private static readonly Guid coverId = Guid.NewGuid();
	private static readonly Guid otherId = Guid.NewGuid();
	[Fact]
	public void CreatesNormalizedDraftWithServerMetadata()
	{
		var before = DateTimeOffset.UtcNow;
		var result = Game.Create(" Elden Ring ", Slug.Create(" elden-ring-2 ").Value!, coverId);

		Assert.True(result.IsSuccess);
		var game = result.Value;

		Assert.Equal(7, game.Id.Version);
		Assert.Equal("Elden Ring", game.Name);
		Assert.Equal("elden-ring-2", game.Slug);
		Assert.Equal(coverId, game.CoverId);
		Assert.Equal(PublicationStatus.Draft, game.Status);
		Assert.Equal(game.CreatedAt, game.UpdatedAt);
		Assert.Equal(TimeSpan.Zero, game.CreatedAt.Offset);
		Assert.InRange(game.CreatedAt, before, DateTimeOffset.UtcNow);
		Assert.NotEqual(game.Id, Game.Create("Other", Slug.Create("other").Value!, coverId).Value!.Id);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void RejectsMissingMetadata(string? value)
	{
		var result = Game.Create(value, Slug.Create(value).Value!, null);

		Assert.True(result.IsFailure);
		Assert.Equal(3, result.Errors.Length);
		Assert.All(result.Errors, error => Assert.Equal(ErrorType.Validation, error.Type));
	}

	[Theory]
	[InlineData("Elden-ring")]
	[InlineData("elden ring")]
	[InlineData("elden--ring")]
	[InlineData("-elden")]
	[InlineData("elden-")]
	[InlineData("игра")]
	[InlineData("elden\nring")]
	public void RejectsInvalidSlugs(string slug) => Assert.True(Game.Create("Game", Slug.Create(slug).Value!, coverId).IsFailure);

	[Fact]
	public void RejectsEmptyImageId()
	{
		var result = Game.Create("Game", Slug.Create("game").Value!, Guid.Empty);

		Assert.Equal(GameErrors.CoverRequired, Assert.Single(result.Errors!));
	}

	[Fact]
	public void DoesNotEnforceNameTechnicalLengthLimit()
	{
		var result = Game.Create(new string('n', 201), Slug.Create("valid").Value!, otherId);

		Assert.True(result.IsSuccess);
	}

}
