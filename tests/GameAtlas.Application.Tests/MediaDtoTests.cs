using System.Text.Json;
using GameAtlas.Application.Media;

namespace GameAtlas.Application.Tests;

public class MediaDtoTests
{
	[Fact]
	public void SharedConcreteAndUnionShapesEmitExactlyOneDiscriminator()
	{
		var image = new ImageAssetDto(Guid.CreateVersion7(), null, "Image", "/media/files/id", "image/png", 10,
			10, 20, "", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
		var video = new VideoAssetDto(Guid.CreateVersion7(), null, "Video", "/media/files/video", "video/mp4", 100,
			image, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
		var folder = new MediaFolderDto(Guid.CreateVersion7(), null, "Folder", 0,
			DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

		AssertType(JsonSerializer.Serialize(image), "image");
		AssertType(JsonSerializer.Serialize<MediaAssetDto>(image), "image");
		AssertType(JsonSerializer.Serialize<MediaItemDto>(image), "image");
		AssertType(JsonSerializer.Serialize(video), "video");
		AssertType(JsonSerializer.Serialize<MediaItemDto>(video), "video");
		AssertType(JsonSerializer.Serialize(folder), "folder");
		AssertType(JsonSerializer.Serialize<MediaItemDto>(folder), "folder");
		Assert.IsType<ImageAssetDto>(JsonSerializer.Deserialize<MediaAssetDto>(
			JsonSerializer.Serialize<MediaAssetDto>(image)));
	}

	private static void AssertType(string json, string type)
	{
		using var document = JsonDocument.Parse(json);

		Assert.Equal(type, document.RootElement.GetProperty("type").GetString());
		Assert.Single(document.RootElement.EnumerateObject(), property => property.Name == "type");
	}
}
