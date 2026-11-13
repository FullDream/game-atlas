using GameAtlas.Domain;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.ValueObjects;

namespace GameAtlas.Domain.Tests;

public class MapSlugTests
{
	[Fact]
	public void SlugUpdatesAreAtomicAndPreserveOmissionsAndNoOps()
	{
		var map = Map.Create(Guid.NewGuid(), "World", Slug.Create("world").Value!).Value!;
		var timestamp = map.UpdatedAt;

		Assert.False(map.Update(slug: Slug.Create(" world ").Value!).Value);
		Assert.Equal(timestamp, map.UpdatedAt);
		Assert.True(map.Update(name: "Changed", slug: Optional<Slug?>.From(null)).IsFailure);
		Assert.Equal("World", map.Name);
		Assert.Equal(timestamp, map.UpdatedAt);
		Assert.True(map.Update(slug: Slug.Create("new-world").Value!).Value);
		Assert.True(map.Rename("Other").IsSuccess);
		Assert.Equal("new-world", map.Slug);
		Assert.True(Map.Create(Guid.NewGuid(), "World", null!).IsFailure);
	}
}
