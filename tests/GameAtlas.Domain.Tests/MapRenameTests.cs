using GameAtlas.Domain.Maps;

namespace GameAtlas.Domain.Tests;

public class MapRenameTests
{
	[Fact]
	public void RenameTrimsAndPreservesGeometryAndCreation()
	{
		var map = Map.Create(Guid.CreateVersion7(), "World", 100, 200).Value!;
		var geometry = map.Geometry;
		var createdAt = map.CreatedAt;
		var result = map.Rename(" New World ");

		Assert.True(result.IsSuccess);
		Assert.True(result.Value);
		Assert.Equal("New World", map.Name);
		Assert.Equal("NEW WORLD", map.NormalizedName);
		Assert.Same(geometry, map.Geometry);
		Assert.Equal(createdAt, map.CreatedAt);
		Assert.True(map.UpdatedAt >= createdAt);
	}

	[Fact]
	public void NoOpPreservesTimestampWhileCaseChangeUpdatesSpelling()
	{
		var map = Map.Create(Guid.CreateVersion7(), "World", 100, 200).Value!;
		var updatedAt = map.UpdatedAt;

		Assert.False(map.Rename(" World ").Value);
		Assert.Equal(updatedAt, map.UpdatedAt);
		Assert.True(map.Rename("world").Value);
		Assert.Equal("world", map.Name);
		Assert.Equal("WORLD", map.NormalizedName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void InvalidRenamePreservesMap(string? name)
	{
		var map = Map.Create(Guid.CreateVersion7(), "World", 100, 200).Value!;
		var updatedAt = map.UpdatedAt;

		Assert.True(map.Rename(name).IsFailure);
		Assert.True(map.Rename(new string('n', 201)).IsFailure);
		Assert.Equal("World", map.Name);
		Assert.Equal(updatedAt, map.UpdatedAt);
		Assert.True(map.Rename(" " + new string('n', 200) + " ").IsSuccess);
	}
}
