using GameAtlas.Application.CategoryGroups.CreateCategoryGroup;
using GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;
using GameAtlas.Domain;

namespace GameAtlas.Application.Tests;

public class CategoryGroupValidationTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData(0, false)]
	[InlineData(-1, false)]
	[InlineData(1, true)]
	[InlineData(int.MaxValue, true)]
	public void ValidatesExplicitOrderForCreationAndOrderOnlyPatch(int? order, bool valid)
	{
		var gameId = Guid.NewGuid();

		Assert.Equal(valid, new CreateCategoryGroupValidation().Validate(
			new CreateCategoryGroup(gameId, Guid.NewGuid(), "Group", order)).IsValid);
		Assert.Equal(valid, new UpdateCategoryGroupValidation().Validate(
			new UpdateCategoryGroup(gameId, Guid.NewGuid(), Guid.NewGuid(), Order: order)).IsValid);
		Assert.True(new CreateCategoryGroupValidation().Validate(new CreateCategoryGroup(gameId, Guid.NewGuid(), "Group")).IsValid);
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
