using System.Data.Common;
using System.Net;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Processing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetVips;

namespace GameAtlas.IntegrationTests;

public class MapTileTests
{
	[Fact]
	public async Task StaticTilesServeGetHeadAndRevalidationWithoutDatabaseQueriesAndHidePrivateFiles()
	{
		var queries = new TileQueries();
		await using var factory = new GameApiFactory(interceptor: queries);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = map.Styles.Single();
		var prefix = MapTestData.TilesPath(map.Id, style.Id);
		var url = $"{prefix}/0/0/0.webp";
		using var queued = await client.GetAsync(url);

		Assert.Equal(HttpStatusCode.NotFound, queued.StatusCode);
		await factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(CancellationToken.None);
		queries.Armed = true;
		using var tile = await client.GetAsync(url);
		using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
		using var head = await client.SendAsync(headRequest);
		using var conditional = new HttpRequestMessage(HttpMethod.Get, url);

		conditional.Headers.IfNoneMatch.Add(tile.Headers.ETag!);
		using var unchanged = await client.SendAsync(conditional);

		Assert.Equal(HttpStatusCode.OK, tile.StatusCode);
		Assert.Equal(HttpStatusCode.OK, head.StatusCode);
		Assert.Empty(await head.Content.ReadAsByteArrayAsync());
		Assert.Equal(tile.Content.Headers.ContentLength, head.Content.Headers.ContentLength);
		Assert.Equal(HttpStatusCode.NotModified, unchanged.StatusCode);
		Assert.Contains("immutable", tile.Headers.CacheControl!.ToString());
		var storage = factory.Services.GetRequiredService<IMediaStorage>();
		var privatePaths = new[]
		{
			$"{prefix}/manifest.json", prefix,
			$"/map-tiles/content/{map.Id:N}/{style.Id:N}/original",
			$"/map-tiles/work/{map.Id:N}/{style.Id:N}/0/0/0.webp",
			"/map-tiles/staging/original.webp", "/map-tiles/not-a-tile.webp"
		};

		Assert.True(File.Exists(storage.AssetPath(style.Source.Id)));

		foreach (var path in privatePaths)
		{
			using var missing = await client.GetAsync(path);

			Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		}

		Assert.Equal(0, queries.Count);
	}

	private sealed class TileQueries : DbCommandInterceptor
	{
		public bool Armed { get; set; }
		public int Count { get; private set; }

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
			CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			if (Armed)
				Count++;

			return ValueTask.FromResult(result);
		}
	}

	[Fact]
	public async Task EveryPublicZoomUsesCorrectOrientationAndTransparentEdgeAndBlankTiles()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		const int width = 1025;
		const int height = 513;
		var pixels = new byte[width * height * 4];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var offset = (y * width + x) * 4;

				pixels[offset] = x < 512 ? (byte)200 : (byte)10;
				pixels[offset + 1] = y < 512 ? (byte)50 : (byte)220;
				pixels[offset + 2] = 100;
				pixels[offset + 3] = x >= 512 && y >= 512 ? (byte)0 : (byte)255;
			}
		}

		using var image = Image.NewFromMemory(pixels, width, height, 4, Enums.BandFormat.Uchar);
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style(image.PngsaveBuffer());
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = map.Styles.Single();
		var geometry = MapGeometry.Create(width, height).Value!;
		var tilesPath = MapTestData.TilesPath(map.Id, style.Id);

		await factory.Services.GetRequiredService<MapProcessor>().RunNextAsync(CancellationToken.None);

		for (var z = 0; z <= geometry.MaxZoom; z++)
		{
			for (var x = 0; x < geometry.Columns(z); x++)
			{
				for (var y = 0; y < geometry.Rows(z); y++)
				{
					using var response = await client.GetAsync($"{tilesPath}/{z}/{x}/{y}.webp");

					Assert.Equal(HttpStatusCode.OK, response.StatusCode);
					using var encoded = Image.NewFromBuffer(await response.Content.ReadAsByteArrayAsync());
					using var tile = encoded.HasAlpha() ? encoded.Copy() : encoded.Bandjoin(255);

					Assert.Equal(512, tile.Width);
					Assert.Equal(512, tile.Height);

					if (x == 0 && y == 0)
						Assert.Equal([200d, 50d, 100d, 255d], tile[0, 0]);

					if (z == geometry.MaxZoom)
					{
						var offset = (y * 512 * width + x * 512) * 4;

						Assert.Equal((double)pixels[offset + 3], tile[0, 0][3]);

						if (pixels[offset + 3] > 0)
							Assert.Equal(pixels.AsSpan(offset, 4).ToArray().Select(value => (double)value), tile[0, 0]);
					}

					if (x == geometry.Columns(z) - 1 || y == geometry.Rows(z) - 1)
						Assert.Equal(0d, tile[511, 511][3]);
				}
			}
		}

		var foreignPath = MapTestData.TilesPath(Guid.NewGuid(), style.Id);
		using var foreign = await client.GetAsync($"{foreignPath}/0/0/0.webp");
		using var outside = await client.GetAsync($"{tilesPath}/0/1/0.webp");
		using var malformed = await client.GetAsync($"{tilesPath}/not-a-number/0/0.webp");

		Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, outside.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
	}
}
