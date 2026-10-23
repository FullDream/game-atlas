using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Markers.Media;

namespace GameAtlas.Domain.Tests;

public class MarkerTests
{
	[Fact]
	public void CreatesAggregateAndPreservesNormalizedNoOpsAndChildIdentity()
	{
		var position = Assert.IsType<Position>(Position.Create(1, -2).Value);
		var image = Assert.IsType<ImageMedia>(ImageMedia.Create(" image ", 1, 2).Value);
		var video = Assert.IsType<VideoMedia>(VideoMedia.Create("video", " ").Value);
		var marker = Assert.IsType<Marker>(Marker.Create(Guid.NewGuid(), " Chest ", position,
			" Description ", [image, video]).Value);
		var before = marker.UpdatedAt;
		var childIds = marker.Media.Select(item => item.Id).ToArray();

		Assert.Equal(7, marker.Id.Version);
		Assert.Equal("Chest", marker.Name);
		Assert.Equal("Description", marker.Description);
		Assert.Equal(marker.CreatedAt, marker.UpdatedAt);
		Assert.Equal(new[] { 0, 1 }, marker.Media.Select(item => item.Order));
		Assert.All(marker.Media, item => Assert.Equal(marker.Id, item.MarkerId));
		Assert.False(marker.Update().Value);
		Assert.False(marker.Update(name: " Chest ", description: " Description ",
			media: new MarkerMedia[]
			{
				Assert.IsType<ImageMedia>(ImageMedia.Create("image", 1, 2).Value),
				Assert.IsType<VideoMedia>(VideoMedia.Create("video").Value)
			}).Value);
		Assert.Equal(before, marker.UpdatedAt);
		Assert.Equal(childIds, marker.Media.Select(item => item.Id));
		Assert.Equal("Chest", Marker.Create(marker.CategoryId, "Chest", position).Value?.Name);
	}

	[Fact]
	public void UpdatesAllFieldsAndReplacesThenClearsMedia()
	{
		var marker = Create();
		var id = marker.Id;
		var created = marker.CreatedAt;
		var destination = Guid.NewGuid();
		var position = Assert.IsType<Position>(Position.Create(10, 20).Value);
		var youtube = Assert.IsType<YouTubeVideoMedia>(YouTubeVideoMedia.Create("dQw4w9WgXcQ").Value);

		Assert.True(marker.Update(destination, "New", " desc ", position,
			new MarkerMedia[] { youtube }).Value);
		Assert.Equal(destination, marker.CategoryId);
		Assert.Equal(position, marker.Position);
		Assert.Equal("New", marker.Name);
		Assert.Equal("desc", marker.Description);
		Assert.Equal(youtube.Id, Assert.Single(marker.Media).Id);
		Assert.True(marker.Update(description: " ", media: Array.Empty<MarkerMedia>()).Value);
		Assert.Null(marker.Description);
		Assert.Empty(marker.Media);
		Assert.Equal(id, marker.Id);
		Assert.Equal(created, marker.CreatedAt);
	}

	[Fact]
	public void InvalidChangesAreAtomicAndChildrenCannotBeReusedAcrossMarkers()
	{
		var marker = Create();
		var child = Assert.IsType<ImageMedia>(ImageMedia.Create("image", 1, 1).Value);
		var other = Assert.IsType<Marker>(Marker.Create(Guid.NewGuid(), "Other", marker.Position,
			media: [child]).Value);
		var before = marker.UpdatedAt;
		var name = marker.Name;

		Assert.True(marker.Update(name: " ", categoryId: Guid.NewGuid()).IsFailure);
		Assert.True(marker.Update(name: "New", position: (Position?)null).IsFailure);
		Assert.True(marker.Update(name: "New", categoryId: (Guid?)null).IsFailure);
		Assert.True(marker.Update(name: "New", description: new string('x', 5001)).IsFailure);
		Assert.True(marker.Update(name: "New", media: new MarkerMedia?[] { null }).IsFailure);
		Assert.True(marker.Update(name: "New", media: new MarkerMedia[] { child }).IsFailure);
		Assert.True(marker.Update(media: (MarkerMedia[]?)null).IsFailure);
		Assert.Equal(before, marker.UpdatedAt);
		Assert.Equal(name, marker.Name);
		Assert.Empty(marker.Media);
		Assert.Equal(other.Id, child.MarkerId);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(200, true)]
	[InlineData(201, false)]
	public void NameBoundsApplyAfterTrimming(int length, bool valid)
	{
		var marker = Create();

		Assert.Equal(valid, marker.Update(name: " " + new string('x', length) + " ").IsSuccess);
	}

	private static Marker Create() => Assert.IsType<Marker>(Marker.Create(Guid.NewGuid(), "Name",
		Assert.IsType<Position>(Position.Create(0, 0).Value)).Value);
}
