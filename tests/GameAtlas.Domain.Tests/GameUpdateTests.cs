using GameAtlas.Domain;
using GameAtlas.Domain.Games;

namespace GameAtlas.Domain.Tests;

public class GameUpdateTests
{
	private static readonly Guid coverId = Guid.NewGuid();
	private static readonly Guid otherId = Guid.NewGuid();
	[Fact]
	public void UpdatesAllMutableFieldsAndPreservesIdentityAndCreationTime()
	{
		var game = Game.Create("Game", "game", coverId).Value!;
		var id = game.Id;
		var createdAt = game.CreatedAt;
		var before = DateTimeOffset.UtcNow;
		var result = game.Update(" Updated ", " updated ", otherId, GameStatus.Published);

		Assert.True(result.IsSuccess);
		Assert.True(result.Value);
		Assert.Equal("Updated", game.Name);
		Assert.Equal("updated", game.Slug);
		Assert.Equal(otherId, game.CoverId);
		Assert.Equal(GameStatus.Published, game.Status);
		Assert.Equal(id, game.Id);
		Assert.Equal(createdAt, game.CreatedAt);
		Assert.Equal(TimeSpan.Zero, game.UpdatedAt.Offset);
		Assert.InRange(game.UpdatedAt, before, DateTimeOffset.UtcNow);
	}

	[Fact]
	public void PreservesOmissionsAndCanReturnToDraft()
	{
		var game = Game.Create("Game", "game", coverId).Value!;

		Assert.True(game.Update(status: GameStatus.Published).Value);
		Assert.True(game.Update(name: "Updated").Value);
		Assert.Equal("game", game.Slug);
		Assert.Equal(coverId, game.CoverId);
		Assert.Equal(GameStatus.Published, game.Status);
		Assert.True(game.Update(status: GameStatus.Draft).Value);
		Assert.Equal(GameStatus.Draft, game.Status);
	}

	[Fact]
	public void DoesNotTouchTimestampForUnspecifiedOrNormalizedEqualValues()
	{
		var game = Game.Create("Game", "game", coverId).Value!;
		var updatedAt = game.UpdatedAt;
		var empty = game.Update();
		var same = game.Update(" Game ", " game ", coverId, GameStatus.Draft);

		Assert.True(empty.IsSuccess);
		Assert.False(empty.Value);
		Assert.True(same.IsSuccess);
		Assert.False(same.Value);
		Assert.Equal(updatedAt, game.UpdatedAt);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	[InlineData("bad--slug")]
	[InlineData("Upper")]
	[InlineData("игра")]
	public void RejectsInvalidMetadataWithoutApplyingAnyChanges(string? slug)
	{
		var game = Game.Create("Game", "game", coverId).Value!;
		var updatedAt = game.UpdatedAt;
		var result = game.Update("New", Optional<string?>.From(slug), otherId, GameStatus.Published);

		Assert.True(result.IsFailure);
		Assert.Contains(result.Errors, error => error.Property == nameof(Game.Slug));
		Assert.Equal("Game", game.Name);
		Assert.Equal("game", game.Slug);
		Assert.Equal(coverId, game.CoverId);
		Assert.Equal(GameStatus.Draft, game.Status);
		Assert.Equal(updatedAt, game.UpdatedAt);
	}

	[Fact]
	public void RejectsRequiredNullsAndUndefinedStatus()
	{
		var game = Game.Create("Game", "game", coverId).Value!;
		var result = game.Update(
			Optional<string?>.From(null), Optional<string?>.From(null), Optional<Guid?>.From(null),
			status: Optional<GameStatus?>.From(null));

		Assert.True(result.IsFailure);
		Assert.Equal(4, result.Errors.Length);
		Assert.Equal(game.CreatedAt, game.UpdatedAt);
		Assert.True(game.Update(status: (GameStatus)99).IsFailure);
		Assert.Equal(GameStatus.Draft, game.Status);
	}

	[Fact]
	public void RejectsEmptyImageWithoutApplyingOtherChanges()
	{
		var game = Game.Create("Game", "game", coverId).Value!;
		var result = game.Update(name: "Changed", coverId: Guid.Empty);

		Assert.Equal(GameErrors.CoverRequired, Assert.Single(result.Errors!));
		Assert.Equal("Game", game.Name);
		Assert.Equal(coverId, game.CoverId);
		Assert.Equal(game.CreatedAt, game.UpdatedAt);
	}

	[Fact]
	public void DoesNotEnforceTechnicalLengthLimits()
	{
		var game = Game.Create("Game", "game", coverId).Value!;
		var result = game.Update(new string('n', 201), new string('s', 201), otherId);

		Assert.True(result.IsSuccess);
		Assert.True(result.Value);
	}
}
