using GameAtlas.Application.Markers;
using GameAtlas.Application.Markers.CreateMarker;
using GameAtlas.Application.Markers.UpdateMarker;
using GameAtlas.Domain;

namespace GameAtlas.Application.Tests;

public class MarkerValidationTests
{
	[Theory]
	[InlineData(1, 0, true)]
	[InlineData(200, 5000, true)]
	[InlineData(201, 0, false)]
	[InlineData(1, 5001, false)]
	[InlineData(0, 0, false)]
	public void EnforcesTrimmedStringBounds(int nameLength, int descriptionLength, bool valid)
	{
		var command = Create() with
		{
			Name = " " + new string('n', nameLength) + " ",
			Description = " " + new string('d', descriptionLength) + " "
		};

		Assert.Equal(valid, new CreateMarkerValidation().Validate(command).IsValid);
	}

	[Fact]
	public void DistinguishesMissingNullZeroAndFiniteCoordinates()
	{
		var validator = new CreateMarkerValidation();

		Assert.False(validator.Validate(Create() with { Position = null }).IsValid);
		Assert.False(validator.Validate(Create() with { Position = new(null, 0) }).IsValid);
		Assert.True(validator.Validate(Create() with { Position = new(0, -0.25) }).IsValid);

		foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
		{
			Assert.False(validator.Validate(Create() with { Position = new(value, 0) }).IsValid);
			Assert.False(validator.Validate(Create() with { Position = new(0, value) }).IsValid);
		}
	}

	[Fact]
	public void RejectsRootEmptyPatchButAllowsEmptyNestedPatchAndNullableDescription()
	{
		var validator = new UpdateMarkerValidation();
		var command = new UpdateMarker(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

		Assert.False(validator.Validate(command).IsValid);
		Assert.True(validator.Validate(command with { Position = new PositionPatch() }).IsValid);
		Assert.True(validator.Validate(command with { Description = Optional<string?>.From(null) }).IsValid);
		Assert.False(validator.Validate(command with { Position = Optional<PositionPatch?>.From(null) }).IsValid);
		Assert.False(validator.Validate(command with { Position = new PositionPatch(Optional<double?>.From(null)) }).IsValid);
		Assert.False(validator.Validate(command with { CategoryId = Optional<Guid?>.From(null) }).IsValid);
		Assert.False(new CreateMarkerValidation().Validate(Create() with { CategoryId = null }).IsValid);
	}

	[Fact]
	public void ValidatesDistinctAssetIdsAndIndexedNulls()
	{
		var id = Guid.NewGuid();
		var validator = new CreateMarkerValidation();

		Assert.True(validator.Validate(Create() with { Media = [id, Guid.NewGuid()] }).IsValid);
		Assert.False(validator.Validate(Create() with { Media = [id, id] }).IsValid);
		Assert.False(validator.Validate(Create() with { Media = [Guid.Empty] }).IsValid);
		Assert.False(validator.Validate(Create() with { Media = null }).IsValid);
		Assert.Contains(validator.Validate(Create() with { Media = [null] }).Errors,
			error => error.PropertyName == "Media[0]");
		var update = new UpdateMarker(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
			Media: Optional<IReadOnlyCollection<Guid?>?>.From([null]));

		Assert.False(new UpdateMarkerValidation().Validate(update).IsValid);
		Assert.True(new UpdateMarkerValidation().Validate(update with
		{
			Media = Optional<IReadOnlyCollection<Guid?>?>.From([])
		}).IsValid);
	}

	private static CreateMarker Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Marker", new(0, 0), null, []);
}
