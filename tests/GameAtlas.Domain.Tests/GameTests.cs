using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;

namespace GameAtlas.Domain.Tests;

public class GameTests
{
	[Fact]
	public void CreatesNormalizedDraftWithServerMetadata()
	{
		var mapId = Guid.NewGuid();
		var before = DateTimeOffset.UtcNow;
		var result = Game.Create(" Elden Ring ", " elden-ring-2 ", " cover ", mapId);

		Assert.True(result.IsSuccess);
		var game = result.Value;

		Assert.Equal(7, game.Id.Version);
		Assert.Equal("Elden Ring", game.Name);
		Assert.Equal("elden-ring-2", game.Slug);
		Assert.Equal("cover", game.Image);
		Assert.Equal(mapId, game.MapId);
		Assert.Equal(GameStatus.Draft, game.Status);
		Assert.Equal(game.CreatedAt, game.UpdatedAt);
		Assert.Equal(TimeSpan.Zero, game.CreatedAt.Offset);
		Assert.InRange(game.CreatedAt, before, DateTimeOffset.UtcNow);
		Assert.NotEqual(game.Id, Game.Create("Other", "other", "image").Value!.Id);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void RejectsMissingMetadata(string? value)
	{
		var result = Game.Create(value, value, value);

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
	public void RejectsInvalidSlugs(string slug) => Assert.True(Game.Create("Game", slug, "image").IsFailure);

	[Fact]
	public void DoesNotEnforceTechnicalLengthLimits()
	{
		var result = Game.Create(new string('n', 201), new string('s', 201), new string('i', 2049));

		Assert.True(result.IsSuccess);
		Assert.Null(result.Value.MapId);
	}

}
