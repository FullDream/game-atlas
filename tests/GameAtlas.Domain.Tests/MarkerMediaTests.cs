using GameAtlas.Domain.Markers.Media;

namespace GameAtlas.Domain.Tests;

public class MarkerMediaTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t ")]
	public void RequiredUrlsRejectNullOrBlank(string? url)
	{
		Assert.True(ImageMedia.Create(url, 1, 1).IsFailure);
		Assert.True(VideoMedia.Create(url).IsFailure);
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(2048, true)]
	[InlineData(2049, false)]
	public void ReferencesValidateTrimmedLength(int length, bool valid)
	{
		var reference = " " + new string('x', length) + " ";

		Assert.Equal(valid, ImageMedia.Create(reference, 1, 1).IsSuccess);
		Assert.Equal(valid, VideoMedia.Create(reference).IsSuccess);
		Assert.Equal(valid, VideoMedia.Create("video", reference).IsSuccess);
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(1, 0)]
	[InlineData(-1, 1)]
	public void ImageDimensionsMustBePositive(int width, int height) =>
		Assert.True(ImageMedia.Create("image", width, height).IsFailure);

	[Fact]
	public void ImageAltDefaultsToEmptyAndPreservesSuppliedText()
	{
		var image = Assert.IsType<ImageMedia>(ImageMedia.Create(" image ", 1920, 1080).Value);

		Assert.Equal("image", image.Url);
		Assert.Equal("", image.Alt);
		Assert.Equal(7, image.Id.Version);
		Assert.Equal(" Caption ", ImageMedia.Create("image", 1, 1, " Caption ").Value?.Alt);
		Assert.True(ImageMedia.Create("image", 1, 1, null).IsFailure);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void VideoPreviewNormalizesAbsentText(string? preview)
	{
		Assert.Null(VideoMedia.Create(" video ", preview).Value?.Preview);
		Assert.Equal("preview", VideoMedia.Create("video", " preview ").Value?.Preview);
	}

	[Theory]
	[InlineData("dQw4w9WgXcQ", true)]
	[InlineData("a_A-1234567", true)]
	[InlineData(null, false)]
	[InlineData("short", false)]
	[InlineData("dQw4w9WgXcQ\n", false)]
	[InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", false)]
	[InlineData("dQw4w9WgXc!", false)]
	public void YouTubeRequiresIdentifierSyntax(string? videoId, bool valid)
	{
		var result = YouTubeVideoMedia.Create(videoId);

		Assert.Equal(valid, result.IsSuccess);

		if (valid)
			Assert.Equal(videoId, result.Value?.VideoId);
	}
}
