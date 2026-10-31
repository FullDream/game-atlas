using GameAtlas.Domain;
using GameAtlas.Domain.Games;

namespace GameAtlas.Domain.Tests;

public class GameMetadataTests
{
	[Fact]
	public void PreservesPlainTextAndDateNormalizesDeveloperAndSupportsClearing()
	{
		var date = new DateOnly(2026, 10, 31);
		var game = Game.Create("Game", "game", Guid.NewGuid(), Guid.NewGuid(), date,
			" Studio ", "Line one\n<b>Line two</b>").Value!;
		Assert.Equal(date, game.ReleaseDate);
		Assert.Equal("Studio", game.Developer);
		Assert.Equal("Line one\n<b>Line two</b>", game.Description);
		var timestamp = game.UpdatedAt;
		Assert.False(game.Update(developer: " Studio ").Value);
		Assert.Equal(timestamp, game.UpdatedAt);
		Assert.True(game.Update(bannerId: Optional<Guid?>.From(null),
			releaseDate: Optional<DateOnly?>.From(null), developer: " ", description: "").Value);
		Assert.Null(game.BannerId);
		Assert.Null(game.ReleaseDate);
		Assert.Null(game.Developer);
		Assert.Null(game.Description);
	}

	[Fact]
	public void InvalidCoverDoesNotApplyOptionalMetadata()
	{
		var game = Game.Create("Game", "game", Guid.NewGuid()).Value!;
		var timestamp = game.UpdatedAt;
		Assert.True(game.Update(coverId: Optional<Guid?>.From(null), developer: "Studio",
			description: "Text", bannerId: Guid.NewGuid()).IsFailure);
		Assert.Null(game.Developer);
		Assert.Null(game.Description);
		Assert.Null(game.BannerId);
		Assert.Equal(timestamp, game.UpdatedAt);
	}
}
