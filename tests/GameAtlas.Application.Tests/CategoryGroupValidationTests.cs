using GameAtlas.Application.Categories.Groups.CreateCategoryGroup;
using GameAtlas.Application.Categories.Groups.UpdateCategoryGroup;
using GameAtlas.Domain;

namespace GameAtlas.Application.Tests;

public class CategoryGroupValidationTests
{
	[Fact]
	public void PlacementOnlyPatchAndOptionalIconAreValid()
	{
		Assert.True(new CreateCategoryGroupValidation().Validate(new CreateCategoryGroup(Guid.NewGuid(), Guid.NewGuid(), "Group", (Guid?)null)).IsValid);
		Assert.True(new UpdateCategoryGroupValidation().Validate(new UpdateCategoryGroup(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BeforeId: (Guid?)null)).IsValid);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(200, true)]
	[InlineData(201, false)]
	public void ValidatesTrimmedNamesForCreateAndRename(int length, bool valid)
	{
		var gameId = Guid.NewGuid();

		var name = " " + new string('x', length) + " ";

		Assert.Equal(valid, new CreateCategoryGroupValidation().Validate(new CreateCategoryGroup(gameId, Guid.NewGuid(), name)).IsValid);
		Assert.Equal(valid, new UpdateCategoryGroupValidation().Validate(
			new UpdateCategoryGroup(gameId, Guid.NewGuid(), Guid.NewGuid(), name)).IsValid);
	}

	[Fact]
	public void RejectsMissingNullAndEmptyPatches()
	{
		var gameId = Guid.NewGuid();

		var validator = new UpdateCategoryGroupValidation();

		Assert.False(new CreateCategoryGroupValidation().Validate(new CreateCategoryGroup(gameId, Guid.NewGuid(), null)).IsValid);
		Assert.False(validator.Validate(new UpdateCategoryGroup(gameId, Guid.NewGuid(), Guid.NewGuid())).IsValid);
		Assert.False(validator.Validate(new UpdateCategoryGroup(gameId, Guid.NewGuid(), Guid.NewGuid(),
			Optional<string?>.From(null))).IsValid);
	}
}
