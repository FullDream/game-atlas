using GameAtlas.Domain.ValueObjects;
using GameAtlas.Application.Maps;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Maps.Processing;

namespace GameAtlas.IntegrationTests;

public class MapProgressTests
{
	[Fact]
	public void PercentagesAreTransientMonotonicAndCannotLeakBetweenAttempts()
	{
		var image = ImageAsset.Create(Guid.CreateVersion7(), "Source", null, "image/png", 100, 1, 1).Value;
		var map = Map.Create(Guid.CreateVersion7(), "Map", Slug.Create("map-" + Guid.NewGuid().ToString("N")).Value!).Value!;
		map.AddStyle(new MapStyleInput("Style", "#fff", image));
		var style = map.Styles.Single();
		var store = new MapProgressStore();
		var attempt = style.AttemptId;

		Assert.Equal(0, store.Read(style));
		Assert.True(style.Claim(attempt).IsSuccess);
		var timestamp = style.UpdatedAt;
		var progress = store.Start(style.Id, attempt);

		progress.Report(60);
		progress.Report(-1);
		Assert.Equal(60, style.ToDto(store).Progress);
		progress.Report(200);
		Assert.Equal(99, store.Read(style));
		Assert.Equal(timestamp, style.UpdatedAt);
		Assert.Equal(0, new MapProgressStore().Read(style));
		Assert.True(style.Fail(attempt).IsSuccess);
		Assert.Equal(99, store.Read(style));
		Assert.True(style.Retry().IsSuccess);
		Assert.Equal(0, store.Read(style));
		Assert.True(style.Claim(style.AttemptId).IsSuccess);
		var next = store.Start(style.Id, style.AttemptId);

		progress.Report(99);
		next.Report(15);
		Assert.Equal(15, store.Read(style));
		Assert.True(style.Publish(attempt).IsFailure);
		Assert.True(style.Publish(style.AttemptId).IsSuccess);
		Assert.Equal(100, new MapProgressStore().Read(style));
	}
}
