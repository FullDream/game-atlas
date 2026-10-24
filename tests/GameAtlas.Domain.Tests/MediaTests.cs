using GameAtlas.Domain;
using GameAtlas.Domain.Media;

namespace GameAtlas.Domain.Tests;

public class MediaTests
{
	[Theory]
	[InlineData(" Icons ", "Icons", "ICONS")]
	[InlineData(" Иконки ", "Иконки", "ИКОНКИ")]
	public void FolderNamesAreTrimmedAndNoOpsKeepTimestamps(string input, string name, string normalized)
	{
		var folder = MediaFolder.Create(input).Value!;
		var timestamp = folder.UpdatedAt;

		Assert.Equal(name, folder.Name);
		Assert.Equal(normalized, folder.NormalizedName);
		Assert.Equal(7, folder.Id.Version);
		Assert.Equal(folder.CreatedAt, timestamp);
		Assert.False(folder.Update(input).Value);
		Assert.Equal(timestamp, folder.UpdatedAt);
		Assert.True(folder.Update(parentId: folder.Id).IsFailure);
		Assert.Null(folder.ParentId);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public void InvalidNamesDoNotPartiallyMoveFolders(string? name)
	{
		var folder = MediaFolder.Create("Icons").Value!;

		Assert.True(folder.Update(name, Guid.NewGuid()).IsFailure);
		Assert.Equal("Icons", folder.Name);
		Assert.Null(folder.ParentId);
		Assert.True(MediaFolder.Create(new string('x', 201)).IsFailure);
		Assert.True(MediaFolder.Create(new string('x', 200)).IsSuccess);
	}

	[Fact]
	public void ImageMetadataIsSharedAndCannotChangeBytes()
	{
		var id = Guid.CreateVersion7();
		var image = ImageAsset.Create(id, " Image ", null, "image/png", 10, 32, 24).Value!;
		var timestamp = image.UpdatedAt;

		Assert.Equal("Image", image.Name);
		Assert.Equal("", image.Alt);
		Assert.False(image.Update(" Image ").Value);
		Assert.Equal(timestamp, image.UpdatedAt);
		Assert.True(image.Update(alt: "<b>text</b>").Value);
		Assert.Equal("<b>text</b>", image.Alt);
		Assert.True(image.Update("Changed", alt: Optional<string?>.From(null)).IsFailure);
		Assert.Equal("Image", image.Name);
		Assert.True(image.Update(previewAssetId: Guid.NewGuid()).IsFailure);
		Assert.Equal(id, image.Id);
		Assert.Equal(10, image.ByteLength);
		Assert.Equal(32d, image.Width);
		Assert.Null(image.Placeholder);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(1.5)]
	public void RasterDimensionsMustBePositiveFiniteIntegers(double width) =>
		Assert.True(ImageAsset.Create(Guid.CreateVersion7(), "Image", null, "image/png", 10, width, 2).IsFailure);

	[Fact]
	public void SvgMayLackIntrinsicDimensionsAndNeverHasPlaceholder()
	{
		Assert.True(ImageAsset.Create(Guid.CreateVersion7(), "Svg", null, "image/svg+xml", 10, null, null).IsSuccess);
		Assert.True(ImageAsset.Create(Guid.CreateVersion7(), "Svg", null, "image/svg+xml", 10, 1.5, 2.5).IsSuccess);
		Assert.True(ImageAsset.Create(Guid.CreateVersion7(), "Svg", null, "image/svg+xml", 10, 10, 10,
			placeholder: "data:image/webp;base64,AA==").IsFailure);
	}

	[Fact]
	public void VideoPreviewCanBeClearedWithoutChangingOriginal()
	{
		var preview = Guid.CreateVersion7();
		var video = VideoAsset.Create(Guid.CreateVersion7(), "Video", null, "video/webm", 100, preview).Value!;

		Assert.Equal(preview, video.PreviewAssetId);
		Assert.True(video.Update(alt: "alt").IsFailure);
		Assert.True(video.Update(previewAssetId: Optional<Guid?>.From(null)).Value);
		Assert.Null(video.PreviewAssetId);
		Assert.Equal("video/webm", video.ContentType);
		Assert.Equal(100, video.ByteLength);
		Assert.True(VideoAsset.Create(Guid.CreateVersion7(), "Video", null, "text/html", 1).IsFailure);
	}
}
