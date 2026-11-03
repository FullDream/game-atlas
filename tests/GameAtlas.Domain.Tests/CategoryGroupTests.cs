using GameAtlas.Domain.Categories;

namespace GameAtlas.Domain.Tests;

public class CategoryGroupTests
{
	[Theory]
	[InlineData(" Resources ", "Resources", "RESOURCES")]
	[InlineData(" Ресурсы ", "Ресурсы", "РЕСУРСЫ")]
	public void CreatesWithNormalizedNameAndImmutableMetadata(string input, string name, string key)
	{
		var gameId = Guid.NewGuid();

		var result = CategoryGroup.Create(gameId, input, 2);

		Assert.True(result.IsSuccess);
		Assert.Equal(7, result.Value.Id.Version);
		Assert.Equal(gameId, result.Value.MapId);
		Assert.Equal(name, result.Value.Name);
		Assert.Equal(key, result.Value.NormalizedName);
		Assert.Equal(2, result.Value.Order);
		Assert.Equal(result.Value.CreatedAt, result.Value.UpdatedAt);
	}

	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	[InlineData(-1)]
	public void InvalidOrderDoesNotPartiallyRename(int? order)
	{
		var group = CategoryGroup.Create(Guid.NewGuid(), "Resources", 1).Value!;

		var before = group.UpdatedAt;

		Assert.True(group.Update("New", order).IsFailure);
		Assert.Equal("Resources", group.Name);
		Assert.Equal("RESOURCES", group.NormalizedName);
		Assert.Equal(1, group.Order);
		Assert.Equal(before, group.UpdatedAt);

		if (order is { } value)
			Assert.True(CategoryGroup.Create(group.MapId, "Group", value).IsFailure);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t ")]
	public void InvalidNameDoesNotPartiallyChangeOrder(string? name)
	{
		var group = CategoryGroup.Create(Guid.NewGuid(), "Resources", 1).Value!;

		var before = group.UpdatedAt;

		Assert.True(group.Update(name, 2).IsFailure);
		Assert.Equal("Resources", group.Name);
		Assert.Equal(1, group.Order);
		Assert.Equal(before, group.UpdatedAt);
	}

	[Fact]
	public void UpdatesEitherFieldOrBothAndPreservesNoOps()
	{
		var group = CategoryGroup.Create(Guid.NewGuid(), "Resources", 1).Value!;

		var created = group.CreatedAt;

		Assert.False(group.Update().Value);
		Assert.False(group.Update(" Resources ", 1).Value);
		Assert.Equal(created, group.UpdatedAt);
		Assert.True(group.Update(order: 10).Value);
		Assert.Equal("Resources", group.Name);
		Assert.Equal(10, group.Order);
		Assert.True(group.Update("New", 5).Value);
		Assert.Equal("New", group.Name);
		Assert.Equal(5, group.Order);
		Assert.Equal(created, group.CreatedAt);

		var updated = group.UpdatedAt;

		Assert.False(group.Update(" New ", 5).Value);
		Assert.Equal(updated, group.UpdatedAt);
	}
}
