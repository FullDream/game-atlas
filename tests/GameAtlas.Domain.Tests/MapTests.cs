using GameAtlas.Domain.Maps;

namespace GameAtlas.Domain.Tests;

public class MapTests
{
	[Fact]
	public void MapNameLengthIsCheckedAfterTrimming()
	{
		Assert.True(Map.Create(Guid.CreateVersion7(), $" {new string('a', 200)} ", 1, 1).IsSuccess);
		Assert.True(Map.Create(Guid.CreateVersion7(), new string('a', 201), 1, 1).IsFailure);
	}

	[Theory]
	[InlineData(" World ", 1, 1)]
	[InlineData("Карта", 16384, 16384)]
	public void CreateWithoutStylesEstablishesImmutableGeometry(string name, int width, int height)
	{
		var created = Map.Create(Guid.CreateVersion7(), name, width, height);

		Assert.True(created.IsSuccess);
		Assert.Equal(name.Trim(), created.Value.Name);
		Assert.Equal(name.Trim().ToUpperInvariant(), created.Value.NormalizedName);
		Assert.Empty(created.Value.Styles);
		Assert.Null(created.Value.DefaultStyleId);
		Assert.Equal(0, created.Value.LastCreationSequence);
		Assert.Equal(width, created.Value.Geometry.Width);
		Assert.Equal(height, created.Value.Geometry.Height);
		Assert.Equal(created.Value.CreatedAt, created.Value.UpdatedAt);
	}

	[Theory]
	[InlineData(null, 1, 1)]
	[InlineData(" ", 1, 1)]
	[InlineData("World", 0, 1)]
	[InlineData("World", 1, 16385)]
	public void RejectInvalidMapMetadata(string? name, int width, int height) =>
		Assert.True(Map.Create(Guid.NewGuid(), name, width, height).IsFailure);

	[Theory]
	[InlineData(MapProcessingStatus.Queued)]
	[InlineData(MapProcessingStatus.Processing)]
	[InlineData(MapProcessingStatus.Ready)]
	[InlineData(MapProcessingStatus.Failed)]
	public void LastStyleCanBeDeletedInEveryStateWithoutLosingGeometry(MapProcessingStatus status)
	{
		var map = Create(Input());
		var style = map.Styles.Single();
		var geometry = map.Geometry;

		if (status != MapProcessingStatus.Queued)
			Assert.True(style.Claim(style.AttemptId).IsSuccess);

		if (status == MapProcessingStatus.Ready)
			Assert.True(style.Publish(style.AttemptId).IsSuccess);
		else if (status == MapProcessingStatus.Failed)
			Assert.True(style.Fail(style.AttemptId).IsSuccess);

		Assert.True(map.DeleteStyle(style.Id).IsSuccess);
		Assert.Empty(map.Styles);
		Assert.Null(map.DefaultStyleId);
		Assert.Same(geometry, map.Geometry);
		Assert.True(map.AddStyle(Input("Next")).IsSuccess);
		Assert.Equal(2, map.Styles.Single().CreationSequence);
	}

	private static MapStyleInput Input(string name = "Atlas", string color = "#abc", int width = 1025,
		int height = 513) => new(name, color, "png", width, height, 100);

	private static Map Create(params MapStyleInput[] inputs)
	{
		var map = Map.Create(Guid.CreateVersion7(), "World", inputs[0].Width, inputs[0].Height).Value!;

		foreach (var input in inputs)
			Assert.True(map.AddStyle(input).IsSuccess);

		return map;
	}

	private static void Publish(MapStyle style)
	{
		Assert.True(style.Claim(style.AttemptId).IsSuccess);
		Assert.True(style.Publish(style.AttemptId).IsSuccess);
	}

	[Fact]
	public void CreationSequenceDeterminesDefaultIndependentlyOfReadiness()
	{
		var map = Create(Input("  Atlas  "), Input("Roads"));
		var styles = map.Styles.ToArray();

		Assert.Equal(7, map.Id.Version);
		Assert.Equal(TimeSpan.Zero, map.CreatedAt.Offset);
		Assert.Equal("Atlas", styles[0].Name);
		Assert.Equal("#AABBCC", styles[0].BackgroundColor);
		Assert.Equal([1L, 2L], styles.Select(style => style.CreationSequence));
		Assert.Null(map.DefaultStyleId);
		Publish(styles[1]);
		Assert.Equal(styles[1].Id, map.DefaultStyleId);
		Publish(styles[0]);
		Assert.Equal(styles[0].Id, map.DefaultStyleId);
		var sharedTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

		foreach (var style in styles)
			typeof(MapStyle).GetProperty(nameof(MapStyle.CreatedAt))!.SetValue(style, sharedTime);

		// EF can restore collections in key order even when creation timestamps tie.
		var storedOrder = (List<MapStyle>)typeof(Map).GetField("styles",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(map)!;

		storedOrder.Reverse();
		Assert.Equal(styles[0].Id, map.DefaultStyleId);
		storedOrder.Reverse();
		Assert.True(map.AddStyle(Input("Third")).IsSuccess);
		Assert.Equal([1L, 2L, 3L], map.Styles.Select(style => style.CreationSequence));
		Assert.True(map.DeleteStyle(styles[0].Id).IsSuccess);
		Assert.Equal(styles[1].Id, map.DefaultStyleId);
		Assert.True(map.DeleteStyle(styles[1].Id).IsSuccess);
		Assert.Null(map.DefaultStyleId);
		Assert.True(map.DeleteStyle(map.Styles.Single().Id).IsSuccess);
		Assert.Empty(map.Styles);
		Assert.Null(map.DefaultStyleId);
	}

	[Fact]
	public void FailedAdditionsLeaveAggregateUntouched()
	{
		var map = Create(Input());
		var timestamp = map.UpdatedAt;

		Assert.True(map.AddStyle(Input(" atlas ")).IsFailure);
		Assert.True(map.AddStyle(Input("Bad", width: 512)).IsFailure);
		Assert.True(map.AddStyle(Input("Bad", "red")).IsFailure);
		Assert.Single(map.Styles);
		Assert.Equal(1, map.LastCreationSequence);
		Assert.Equal(timestamp, map.UpdatedAt);
	}

	[Fact]
	public void PreparedStyleOwnershipAndDuplicateIdentityHaveDistinctErrorsWithoutMutation()
	{
		var map = Create(Input());
		var foreign = Create(Input("Foreign", width: 512)).Styles.Single();
		var existing = map.Styles.Single();
		var timestamp = map.UpdatedAt;
		var sequence = map.LastCreationSequence;
		var wrongOwner = map.AppendPreparedStyle(foreign);
		var duplicate = map.AppendPreparedStyle(existing);

		Assert.Equal(MapErrors.StyleOwnershipMismatch, Assert.Single(wrongOwner.Errors!));
		Assert.Equal(MapErrors.DuplicateStyle, Assert.Single(duplicate.Errors!));
		Assert.Single(map.Styles);
		Assert.Equal(sequence, map.LastCreationSequence);
		Assert.Equal(timestamp, map.UpdatedAt);
		Assert.Equal(1, existing.CreationSequence);
		Assert.Equal(1, foreign.CreationSequence);
	}

	[Fact]
	public void GeometryRemainsTheSameValueForTheLifetimeOfTheMap()
	{
		var map = Create(Input());
		var geometry = map.Geometry;

		Assert.True(map.AddStyle(Input("Roads")).IsSuccess);
		Assert.Same(geometry, map.Geometry);
		Assert.Equal(map.Width, geometry.Width);
		Assert.Equal(map.Height, geometry.Height);
	}

	[Theory]
	[InlineData("", "#fff")]
	[InlineData("  ", "#fff")]
	[InlineData(null, "#fff")]
	[InlineData("Atlas", null)]
	[InlineData("Atlas", "#ffff")]
	[InlineData("Atlas", "#ffg")]
	[InlineData("Atlas", "#fff\n")]
	public void InvalidMetadataIsRejected(string? name, string? color)
	{
		var input = Input() with { Name = name, BackgroundColor = color };

		var map = Map.Create(Guid.NewGuid(), "World", input.Width, input.Height).Value!;

		Assert.True(map.AddStyle(input).IsFailure);
	}

	[Fact]
	public void MetadataPatchIsAtomicAndNormalizedNoOpDoesNotWrite()
	{
		var map = Create(Input(), Input("Roads"));
		var style = map.Styles.First();
		var timestamp = style.UpdatedAt;
		var mapTimestamp = map.UpdatedAt;

		Assert.False(map.UpdateStyle(style.Id, " Atlas ", "#aabbcc").Value);
		Assert.Equal(timestamp, style.UpdatedAt);
		Assert.Equal(mapTimestamp, map.UpdatedAt);
		Assert.True(map.UpdateStyle(style.Id, "Renamed", "invalid").IsFailure);
		Assert.Equal("Atlas", style.Name);
		Assert.True(map.UpdateStyle(style.Id, " roads ").IsFailure);
		Assert.True(map.UpdateStyle(style.Id, new string('x', 201)).IsFailure);
		Assert.True(map.UpdateStyle(style.Id, backgroundColor: "#fff").Value);
		Assert.Equal("#FFFFFF", style.BackgroundColor);
		Assert.Equal(MapProcessingStatus.Queued, style.Status);
	}

	[Fact]
	public void ProgressPublicationFailureAndRetryFenceObsoleteAttempts()
	{
		var map = Create(Input(), Input("Other"));
		var style = map.Styles.First();
		var attempt = style.AttemptId;
		var other = map.Styles.Last();

		Assert.True(style.ReportProgress(attempt, 50).IsFailure);
		Assert.True(style.Claim(attempt).IsSuccess);
		Assert.True(style.Claim(attempt).IsFailure);
		Assert.True(style.ReportProgress(attempt, 60).IsSuccess);
		Assert.True(style.ReportProgress(attempt, -1).IsSuccess);
		Assert.Equal(60, style.Progress);
		Assert.True(style.ReportProgress(attempt, 200).IsSuccess);
		Assert.Equal(99, style.Progress);
		Assert.True(other.Publish(attempt).IsFailure);
		Assert.True(style.Publish(Guid.NewGuid()).IsFailure);
		Assert.Null(map.DefaultStyleId);
		Assert.True(style.Fail(attempt).IsSuccess);
		Assert.Equal(99, style.Progress);
		Assert.NotNull(style.ErrorMessage);
		Assert.True(style.Retry().IsSuccess);
		Assert.NotEqual(attempt, style.AttemptId);
		Assert.Equal(0, style.Progress);
		Assert.Null(style.ErrorCode);
		Assert.True(style.Fail(attempt).IsFailure);
		Assert.True(style.Retry().IsFailure);
		Publish(style);
		Assert.Equal(100, style.Progress);
		Assert.Equal(style.Id, map.DefaultStyleId);
		Assert.True(style.Retry().IsFailure);
	}

	[Fact]
	public void RecoveryResetsOnlyInterruptedWorkAndPreservesOriginalMetadata()
	{
		var map = Create(Input());
		var style = map.Styles.Single();

		Assert.False(style.Recover());
		var attempt = style.AttemptId;

		Assert.True(style.Claim(attempt).IsSuccess);
		Assert.True(style.ReportProgress(attempt, 80).IsSuccess);
		Assert.True(style.Recover());
		Assert.NotEqual(attempt, style.AttemptId);
		Assert.Equal(0, style.Progress);
		Assert.Null(map.DefaultStyleId);
		Assert.Equal("png", style.Format);
		Assert.Equal(100, style.ByteLength);
		Assert.True(style.Publish(attempt).IsFailure);
		Assert.True(style.Claim(style.AttemptId).IsSuccess);
		Assert.True(style.Fail(style.AttemptId).IsSuccess);
		Assert.False(style.Recover());
		Assert.Null(map.DefaultStyleId);
		Assert.True(style.Retry().IsSuccess);
		Publish(style);
		Assert.Equal(style.Id, map.DefaultStyleId);
		Assert.False(style.Recover());
	}
}
