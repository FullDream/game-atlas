using GameAtlas.Domain.Markers;

namespace GameAtlas.Domain.Tests;

public class MarkerTests
{
	[Fact]
	public void CreatesAggregateAndPreservesNormalizedNoOpsAndAttachmentIdentity()
	{
		var ids = new Guid?[] { Guid.NewGuid(), Guid.NewGuid() };
		var position = Position.Create(1, -2).Value!;
		var marker = Marker.Create(Guid.NewGuid(), " Chest ", position, " Description ", ids).Value!;
		var before = marker.UpdatedAt;
		var attachments = marker.Media.ToArray();

		Assert.Equal(7, marker.Id.Version);
		Assert.Equal("Chest", marker.Name);
		Assert.Equal("Description", marker.Description);
		Assert.Equal(marker.CreatedAt, marker.UpdatedAt);
		Assert.Equal(new[] { 0, 1 }, marker.Media.Select(item => item.Order));
		Assert.All(marker.Media, item => Assert.Equal(marker.Id, item.MarkerId));
		Assert.False(marker.Update().Value);
		Assert.False(marker.Update(name: " Chest ", description: " Description ", media: ids).Value);
		Assert.Equal(before, marker.UpdatedAt);
		Assert.Same(attachments[0], marker.Media.First());
	}

	[Fact]
	public void ReorderingRetainsExistingRelationshipsAndClearingDoesNotChangeIdentityOrCreationTime()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		var marker = Marker.Create(Guid.NewGuid(), "Name", Position.Create(0, 0).Value, media: [first, second]).Value!;
		var id = marker.Id;
		var created = marker.CreatedAt;
		var firstAttachment = marker.Media.First();

		Assert.True(marker.Update(media: new Guid?[] { second, first }).Value);
		Assert.Equal(new[] { second, first }, marker.Media.Select(item => item.AssetId));
		Assert.Equal(new[] { 0, 1 }, marker.Media.Select(item => item.Order));
		Assert.Same(firstAttachment, marker.Media.Last());
		Assert.True(marker.Update(name: "New", description: " ", media: Array.Empty<Guid?>()).Value);
		Assert.Null(marker.Description);
		Assert.Empty(marker.Media);
		Assert.Equal(id, marker.Id);
		Assert.Equal(created, marker.CreatedAt);
	}

	[Fact]
	public void InvalidChangesAreAtomicAndLibraryAssetsCanBeReusedAcrossMarkers()
	{
		var marker = Create();
		var assetId = Guid.NewGuid();
		var other = Marker.Create(Guid.NewGuid(), "Other", marker.Position, media: [assetId]).Value!;
		var before = marker.UpdatedAt;

		Assert.True(marker.Update(name: " ", categoryId: Guid.NewGuid()).IsFailure);
		Assert.True(marker.Update(name: "New", position: (Position?)null).IsFailure);
		Assert.True(marker.Update(name: "New", categoryId: (Guid?)null).IsFailure);
		Assert.True(marker.Update(name: "New", description: new string('x', 5001)).IsFailure);
		Assert.True(marker.Update(name: "New", media: new Guid?[] { null }).IsFailure);
		Assert.True(marker.Update(name: "New", media: new Guid?[] { Guid.Empty }).IsFailure);
		Assert.True(marker.Update(name: "New", media: new Guid?[] { assetId, assetId }).IsFailure);
		Assert.True(marker.Update(media: (Guid?[]?)null).IsFailure);
		Assert.Equal(before, marker.UpdatedAt);
		Assert.Equal("Name", marker.Name);
		Assert.Empty(marker.Media);
		Assert.True(marker.Update(media: new Guid?[] { assetId }).Value);
		Assert.Equal(assetId, Assert.Single(other.Media).AssetId);
		Assert.Equal(assetId, Assert.Single(marker.Media).AssetId);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(200, true)]
	[InlineData(201, false)]
	public void NameBoundsApplyAfterTrimming(int length, bool valid) =>
		Assert.Equal(valid, Create().Update(name: " " + new string('x', length) + " ").IsSuccess);

	private static Marker Create() => Marker.Create(Guid.NewGuid(), "Name", Position.Create(0, 0).Value).Value!;
}
