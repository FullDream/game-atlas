using GameAtlas.Application.Markers;
using GameAtlas.Application.Markers.CreateMarker;
using GameAtlas.Application.Markers.Media;
using GameAtlas.Application.Markers.Media.Image;
using GameAtlas.Application.Markers.Media.Video;
using GameAtlas.Application.Markers.Media.YouTubeVideo;
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
	public void ValidatesEverySubtypeWithIndexedErrorsAndAllowsDuplicateAttachments()
	{
		var image = new ImageMediaInput(" image ", 1, int.MaxValue);
		var validator = new CreateMarkerValidation();
		var valid = Create() with
		{
			Media = [image, image, new VideoMediaInput("video", " "), new YouTubeVideoMediaInput("Ab_cd-Ef012")]
		};

		Assert.True(validator.Validate(valid).IsValid);
		Assert.False(validator.Validate(Create() with { Media = null }).IsValid);
		var invalid = validator.Validate(Create() with
		{
			Media = [null, image with { Alt = null }, image with { Width = 0 },
				new VideoMediaInput(" ", new string('x', 2049)), new YouTubeVideoMediaInput("invalid")]
		});

		Assert.False(invalid.IsValid);

		foreach (var index in Enumerable.Range(0, 5))
			Assert.Contains(invalid.Errors, error => error.PropertyName.StartsWith($"Media[{index}]"));

		var update = new UpdateMarker(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
			Media: Optional<IReadOnlyCollection<MarkerMediaInput?>?>.From([null]));

		Assert.Contains(new UpdateMarkerValidation().Validate(update).Errors,
			error => error.PropertyName.StartsWith("Media[0]"));

		Assert.True(new UpdateMarkerValidation().Validate(update with
		{
			Media = Optional<IReadOnlyCollection<MarkerMediaInput?>?>.From([])
		}).IsValid);
	}

	[Theory]
	[InlineData(2048, true)]
	[InlineData(2049, false)]
	public void ReferenceBoundsApplyToImageVideoAndPreview(int length, bool valid)
	{
		var reference = new string('x', length);
		var command = Create() with
		{
			Media = [new ImageMediaInput(reference, 1, 1), new VideoMediaInput(reference, reference)]
		};

		Assert.Equal(valid, new CreateMarkerValidation().Validate(command).IsValid);
	}

	private static CreateMarker Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Marker", new(0, 0), null, []);
}
