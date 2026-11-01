using System.Net;
using System.Net.Http.Json;
using System.Text;
using GameAtlas.Application.Maps;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapPresentationTests
{
	[Theory]
	[InlineData(1000, 8000)]
	[InlineData(8000, 1000)]
	public async Task DifferentResolutionsUseNativePyramidsWithoutChangingLogicalGeometry(int first, int second)
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "native");
		using var client = factory.CreateHttpsClient();
		using var initial = MapTestData.Style(MapTestData.ImageBytes(first, 17));
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, initial);
		var map = await MapTestData.ReadAsync(created);
		using var additional = MapTestData.Style(MapTestData.ImageBytes(second, 19), "Second");
		using var attached = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), additional);
		var result = await MapTestData.ReadAsync(attached);
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		var ready = (await client.GetFromJsonAsync<MapDto>(MapTestData.PathFor(game.Id), MapTestData.JsonOptions))!;
		var style = ready.Styles.Last();

		Assert.Equal(first, ready.Width);
		Assert.Equal(17, ready.Height);
		Assert.Equal(map.Extent, ready.Extent);
		Assert.Equal(second, style.Source.Width);
		Assert.Equal(second / 2d, style.TileGeometry.Extent.ElementAt(2));
		Assert.Equal(Math.Ceiling(Math.Log2(second / 512d)), style.TileGeometry.MaxZoom);
		Assert.Equal(1d, style.TileGeometry.Resolutions.Last());
		var lastTile = style.TilesUrlTemplate!.Replace("{z}", style.TileGeometry.MaxZoom.ToString())
			.Replace("{x}", ((second - 1) / 512).ToString()).Replace("{y}", "0");
		using var tile = await client.GetAsync(lastTile);

		tile.EnsureSuccessStatusCode();
	}

	[Theory]
	[InlineData("application/json")]
	[InlineData("application/merge-patch+json")]
	public async Task DefaultAndOrderPersistIndependentlyAndInvalidPatchesAreAtomic(string mediaType)
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "presentation");
		using var client = factory.CreateHttpsClient();
		using var created = await MapTestData.CreateWithStylesAsync(client, game.Id);
		var map = await MapTestData.ReadAsync(created);
		var ids = map.Styles.Select(style => style.Id).ToArray();
		var path = MapTestData.PathFor(game.Id);
		using var notReady = await client.PatchAsJsonAsync(path, new { defaultStyleId = ids[1] });

		Assert.Equal(HttpStatusCode.UnprocessableEntity, notReady.StatusCode);
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var body = new StringContent($$"""{"styleOrder":["{{ids[1]}}","{{ids[0]}}"]}""", Encoding.UTF8, mediaType);
		using var reordered = await client.PatchAsync(path, body);
		var ordered = await MapTestData.ReadAsync(reordered);

		Assert.Equal(ids.Reverse(), ordered.Styles.Select(style => style.Id));
		Assert.Equal(ids[0], ordered.DefaultStyleId);
		using var selected = await client.PatchAsJsonAsync(path, new { defaultStyleId = ids[1] });
		var preferred = await MapTestData.ReadAsync(selected);
		using var noOp = await client.PatchAsJsonAsync(path, new { defaultStyleId = ids[1] });

		Assert.Equal(preferred.UpdatedAt, (await MapTestData.ReadAsync(noOp)).UpdatedAt);

		foreach (var patch in new object[]
		{
			new { name = "Changed", styleOrder = new[] { ids[0] } },
			new { name = "Changed", styleOrder = new[] { ids[0], ids[0] } },
			new { name = "Changed", styleOrder = new Guid?[] { ids[0], null } },
			new { name = "Changed", defaultStyleId = (Guid?)null }
		})
		{
			using var invalid = await client.PatchAsJsonAsync(path, patch);

			Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
		}

		using var foreign = await client.PatchAsJsonAsync(path, new { name = "Changed", defaultStyleId = Guid.NewGuid() });

		Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
		var read = (await client.GetFromJsonAsync<MapDto>(path, MapTestData.JsonOptions))!;

		Assert.Equal(map.Name, read.Name);
		Assert.Equal(ids[1], read.DefaultStyleId);
		Assert.Equal(ids.Reverse(), read.Styles.Select(style => style.Id));
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var stored = await context.Maps.SingleAsync(item => item.Id == map.Id);

		Assert.Equal(ids[1], stored.DefaultStyleId);
		using var deleted = await client.DeleteAsync(path + $"/styles/{ids[1]}");

		deleted.EnsureSuccessStatusCode();
		var remaining = (await client.GetFromJsonAsync<MapDto>(path, MapTestData.JsonOptions))!;

		Assert.Equal(ids[0], remaining.DefaultStyleId);
	}

	[Fact]
	public async Task ConcurrentFirstAttachmentsInitializeOnceAndFailuresLeaveNoGeometry()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "concurrent");
		using var client = factory.CreateHttpsClient();
		using var creation = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "World" });
		var blank = await MapTestData.ReadAsync(creation);
		var path = MapTestData.PathFor(game.Id, blank.Id);
		using var invalid = await client.PostAsJsonAsync(path + "/styles", new
		{
			name = "Invalid",
			backgroundColor = "red",
			sourceAssetId = Guid.NewGuid()
		});
		var unchanged = (await client.GetFromJsonAsync<MapDto>(path, MapTestData.JsonOptions))!;

		Assert.Null(unchanged.Width);
		using var first = MapTestData.Style(MapTestData.ImageBytes(1025, 513), "First");
		using var second = MapTestData.Style(MapTestData.ImageBytes(513, 1025), "Second");
		var replies = await Task.WhenAll(MapTestData.AddStyleAsync(client, path, first), MapTestData.AddStyleAsync(client, path, second));

		try
		{
			Assert.All(replies, reply => reply.EnsureSuccessStatusCode());
			var initialized = (await client.GetFromJsonAsync<MapDto>(path, MapTestData.JsonOptions))!;

			Assert.Equal(2, initialized.Styles.Count);
			Assert.Equal(initialized.Styles.First().Source.Width, initialized.Width);
			Assert.Equal(initialized.Styles.First().Source.Height, initialized.Height);
		}
		finally
		{
			foreach (var reply in replies)
				reply.Dispose();
		}
	}
}
