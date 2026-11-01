using GameAtlas.Application.Maps.UpdateMap;
using GameAtlas.Domain;

namespace GameAtlas.Application.Tests;

public class UpdateMapValidationTests
{
	[Fact]
	public async Task PatchRequiresSpecifiedNonNullTrimmedName()
	{
		var validator = new UpdateMapValidation();
		var gameId = Guid.CreateVersion7();
		var mapId = Guid.CreateVersion7();

		Assert.False((await validator.ValidateAsync(new UpdateMap(gameId, mapId))).IsValid);
		Assert.False((await validator.ValidateAsync(new UpdateMap(gameId, mapId, Optional<string?>.From(null)))).IsValid);
		Assert.False((await validator.ValidateAsync(new UpdateMap(gameId, mapId, "  "))).IsValid);
		Assert.False((await validator.ValidateAsync(new UpdateMap(gameId, mapId, new string('n', 201)))).IsValid);
		Assert.True((await validator.ValidateAsync(new UpdateMap(gameId, mapId, " " + new string('n', 200) + " "))).IsValid);
	}
}
