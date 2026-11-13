using GameAtlas.Application.Maps.CreateMap;
using GameAtlas.Application.Maps.UpdateMap;
using GameAtlas.Domain;

namespace GameAtlas.Application.Tests;

public class MapSlugValidationTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("Upper")]
	[InlineData("bad--slug")]
	public void RejectsInvalidCreationAndSpecifiedPatch(string? slug)
	{
		var create = new CreateMapValidation().Validate(new CreateMap(Guid.NewGuid(), "World", slug));
		var patch = new UpdateMapValidation().Validate(new UpdateMap(Guid.NewGuid(), Guid.NewGuid(), Slug: Optional<string?>.From(slug)));

		Assert.Contains(create.Errors, error => error.PropertyName == "Slug");
		Assert.Contains(patch.Errors, error => error.PropertyName == "Slug");
	}

	[Fact]
	public void AcceptsSlugOnlyPatchAndRejectsMissingName()
	{
		Assert.True(new UpdateMapValidation().Validate(new UpdateMap(Guid.NewGuid(), Guid.NewGuid(), Slug: " world ")).IsValid);
		Assert.Contains(new CreateMapValidation().Validate(new CreateMap(Guid.NewGuid(), null, "world")).Errors,
			error => error.PropertyName == "Name");
	}
}
