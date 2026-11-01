using GameAtlas.Application.Maps.CreateMap;
using GameAtlas.Application.Maps.UpdateMapStyle;
using GameAtlas.Application.Maps.AddMapStyle;
using GameAtlas.Domain;

namespace GameAtlas.Application.Tests;

public class MapValidationTests
{
	[Fact]
	public async Task MetadataTrimsNamesAndEnforcesColorAndLengthBoundaries()
	{
		var validator = new AddMapStyleValidation();
		var valid = await validator.ValidateAsync(new AddMapStyle(Guid.NewGuid(), Guid.NewGuid(),
			" " + new string('n', 200) + " ", "#abc", Guid.NewGuid()));

		var invalid = await validator.ValidateAsync(
			new AddMapStyle(Guid.NewGuid(), Guid.NewGuid(), new string('n', 201), "#ggg", Guid.NewGuid()));

		Assert.True(valid.IsValid);
		Assert.Equal(new[] { "BackgroundColor", "Name" },
			invalid.Errors.Select(error => error.PropertyName).Distinct().Order());
	}

	[Fact]
	public async Task PatchDistinguishesEmptyNullAndOneSpecifiedField()
	{
		var validator = new UpdateMapStyleValidation();
		var empty = await validator.ValidateAsync(new UpdateMapStyle(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
		var nullName = await validator.ValidateAsync(new UpdateMapStyle(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
			Optional<string?>.From(null)));
		var one = await validator.ValidateAsync(
			new UpdateMapStyle(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BackgroundColor: "#AbC"));

		Assert.False(empty.IsValid);
		Assert.False(nullName.IsValid);
		Assert.True(one.IsValid);
		Assert.Contains(nullName.Errors, error => error.PropertyName == "Name");
	}

	[Fact]
	public async Task CreationFailuresUseFlatMetadataPaths()
	{
		var command = new CreateMap(Guid.NewGuid(), " ");
		var result = await new CreateMapValidation().ValidateAsync(command);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.PropertyName == "Name");
	}
}
