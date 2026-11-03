using GameAtlas.Domain.Categories;

namespace GameAtlas.Domain.Tests;

public class CategoryTests
{
	[Theory]
	[InlineData(" Resources ", "Resources", "RESOURCES")]
	[InlineData(" Ресурсы ", "Ресурсы", "РЕСУРСЫ")]
	public void CreationNormalizesMetadata(string input, string name, string normalized)
	{
		var groupId = Guid.NewGuid();

		var iconId = Guid.NewGuid();

		var category = Assert.IsType<Category>(
			Category.Create(groupId, input, iconId, "#ff8800", 2).Value);

		Assert.Equal(7, category.Id.Version);
		Assert.Equal(groupId, category.CategoryGroupId);
		Assert.Equal(name, category.Name);
		Assert.Equal(normalized, category.NormalizedName);
		Assert.Equal(iconId, category.IconAssetId);
		Assert.Equal("#FF8800", category.Color);
		Assert.Equal(2, category.Order);
		Assert.Equal(category.CreatedAt, category.UpdatedAt);
		Assert.Equal(TimeSpan.Zero, category.CreatedAt.Offset);
	}

	[Theory]
	[InlineData(null, "icon", "#123456", 1)]
	[InlineData(" ", "icon", "#123456", 1)]
	[InlineData("Name", "icon", null, 1)]
	[InlineData("Name", "icon", "red", 1)]
	[InlineData("Name", "icon", "#12", 1)]
	[InlineData("Name", "icon", "#1234", 1)]
	[InlineData("Name", "icon", "#123\n", 1)]
	[InlineData("Name", "icon", "#12345678", 1)]
	[InlineData("Name", "icon", " #123456", 1)]
	[InlineData("Name", "icon", "#123456\n", 1)]
	[InlineData("Name", "icon", "#GGGGGG", 1)]
	[InlineData("Name", "icon", "#123456", 0)]
	[InlineData("Name", "icon", "#123456", -1)]
	public void InvalidStateDoesNotPartiallyMutate(string? name, string? icon, string? color, int order)
	{
		var category = Assert.IsType<Category>(
			Category.Create(Guid.NewGuid(), "Original", Guid.NewGuid(), "#ABCDEF", 5).Value);

		var before = Snapshot(category);
		Guid? iconId = icon is null ? null : string.IsNullOrWhiteSpace(icon) ? Guid.Empty : Guid.NewGuid();

		Assert.True(Category.Create(Guid.NewGuid(), name, iconId, color, order).IsFailure);
		Assert.True(category.Update(name, iconId, color, order, Guid.NewGuid()).IsFailure);
		Assert.Equal(before, Snapshot(category));
	}

	[Fact]
	public void UpdatesEachFieldAndMembershipAndPreservesNormalizedNoOps()
	{
		var category = Assert.IsType<Category>(Category.Create(Guid.NewGuid(), "Name", Guid.NewGuid(), "#ABCDEF", 1).Value);

		var id = category.Id;

		var created = category.CreatedAt;

		var before = Snapshot(category);

		Assert.False(category.Update().Value);
		Assert.False(category.Update(" Name ", category.IconAssetId, "#abcdef", 1, category.CategoryGroupId).Value);
		Assert.Equal(before, Snapshot(category));
		Assert.True(category.Update(name: "New").Value);
		Assert.True(category.Update(iconAssetId: Guid.NewGuid()).Value);
		Assert.True(category.Update(color: "#112233").Value);
		Assert.True(category.Update(order: 10).Value);

		var destination = Guid.NewGuid();

		Assert.True(category.Update(categoryGroupId: destination).Value);
		Assert.Equal(destination, category.CategoryGroupId);
		Assert.True(category.Update("Combined", Guid.NewGuid(), "#445566", 2, Guid.NewGuid()).Value);
		Assert.Equal(id, category.Id);
		Assert.Equal(created, category.CreatedAt);
		Assert.True(category.Update(order: (int?)null).IsFailure);
		Assert.True(category.Update(categoryGroupId: (Guid?)null).IsFailure);
	}

	[Theory]
	[InlineData("#fff", "#FFFFFF")]
	[InlineData("#F80", "#FF8800")]
	[InlineData("#aBc", "#AABBCC")]
	[InlineData("#000", "#000000")]
	public void ShortColorsExpandAndEquivalentUpdatesPreserveTimestamp(string input, string expected)
	{
		var category = Assert.IsType<Category>(Category.Create(Guid.NewGuid(), "Name", Guid.NewGuid(), input, 1).Value);

		var before = Snapshot(category);

		Assert.Equal(expected, category.Color);
		Assert.False(category.Update(color: input).Value);
		Assert.False(category.Update(color: expected).Value);
		Assert.Equal(before, Snapshot(category));
		Assert.True(category.Update(color: expected == "#000000" ? "#fff" : "#000").Value);
		Assert.Equal(expected == "#000000" ? "#FFFFFF" : "#000000", category.Color);
	}

	private static object Snapshot(Category category) => new
	{
		category.Id,
		category.CategoryGroupId,
		category.Name,
		category.NormalizedName,
		category.IconAssetId,
		category.Color,
		category.Order,
		category.CreatedAt,
		category.UpdatedAt
	};
}
