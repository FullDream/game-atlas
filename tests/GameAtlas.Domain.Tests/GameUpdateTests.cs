using GameAtlas.Domain;
using GameAtlas.Domain.Games;

namespace GameAtlas.Domain.Tests;

public class GameUpdateTests
{
	[Fact]
	public void UpdatesAllMutableFieldsAndPreservesIdentityAndCreationTime()
	{
		var game = Game.Create("Game", "game", "cover").Value!;
		var id = game.Id;
		var createdAt = game.CreatedAt;
		var mapId = Guid.NewGuid();
		var before = DateTimeOffset.UtcNow;
		var result = game.Update(" Updated ", " updated ", " new-cover ", mapId, GameStatus.Published);

		Assert.True(result.IsSuccess);
		Assert.True(result.Value);
		Assert.Equal("Updated", game.Name);
		Assert.Equal("updated", game.Slug);
		Assert.Equal("new-cover", game.Image);
		Assert.Equal(mapId, game.MapId);
		Assert.Equal(GameStatus.Published, game.Status);
		Assert.Equal(id, game.Id);
		Assert.Equal(createdAt, game.CreatedAt);
		Assert.Equal(TimeSpan.Zero, game.UpdatedAt.Offset);
		Assert.InRange(game.UpdatedAt, before, DateTimeOffset.UtcNow);
	}

	[Fact]
	public void PreservesOmissionsAndCanClearMapAndReturnToDraft()
	{
		var mapId = Guid.NewGuid();
		var game = Game.Create("Game", "game", "cover", mapId).Value!;

		Assert.True(game.Update(status: GameStatus.Published).Value);
		Assert.True(game.Update(name: "Updated").Value);
		Assert.Equal("game", game.Slug);
		Assert.Equal("cover", game.Image);
		Assert.Equal(mapId, game.MapId);
		Assert.Equal(GameStatus.Published, game.Status);
		Assert.True(game.Update(mapId: Optional<Guid?>.From(null), status: GameStatus.Draft).Value);
		Assert.Null(game.MapId);
		Assert.Equal(GameStatus.Draft, game.Status);
	}

	[Fact]
	public void DoesNotTouchTimestampForUnspecifiedOrNormalizedEqualValues()
	{
		var game = Game.Create("Game", "game", "cover").Value!;
		var updatedAt = game.UpdatedAt;
		var empty = game.Update();
		var same = game.Update(" Game ", " game ", " cover ", Optional<Guid?>.From(null), GameStatus.Draft);

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
		var game = Game.Create("Game", "game", "cover").Value!;
		var updatedAt = game.UpdatedAt;
		var result = game.Update("New", Optional<string?>.From(slug), "new-cover", Guid.NewGuid(), GameStatus.Published);

		Assert.True(result.IsFailure);
		Assert.Contains(result.Errors, error => error.Property == nameof(Game.Slug));
		Assert.Equal("Game", game.Name);
		Assert.Equal("game", game.Slug);
		Assert.Equal("cover", game.Image);
		Assert.Null(game.MapId);
		Assert.Equal(GameStatus.Draft, game.Status);
		Assert.Equal(updatedAt, game.UpdatedAt);
	}

	[Fact]
	public void RejectsRequiredNullsAndUndefinedStatus()
	{
		var game = Game.Create("Game", "game", "cover").Value!;
		var result = game.Update(
			Optional<string?>.From(null), Optional<string?>.From(null), Optional<string?>.From(null),
			status: Optional<GameStatus?>.From(null));

		Assert.True(result.IsFailure);
		Assert.Equal(4, result.Errors.Length);
		Assert.Equal(game.CreatedAt, game.UpdatedAt);
		Assert.True(game.Update(status: (GameStatus)99).IsFailure);
		Assert.Equal(GameStatus.Draft, game.Status);
	}

	[Fact]
	public void DoesNotEnforceTechnicalLengthLimits()
	{
		var game = Game.Create("Game", "game", "cover").Value!;
		var result = game.Update(new string('n', 201), new string('s', 201), new string('i', 2049));

		Assert.True(result.IsSuccess);
		Assert.True(result.Value);
	}
}
