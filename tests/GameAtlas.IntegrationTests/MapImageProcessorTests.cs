using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Images;
using NetVips;

namespace GameAtlas.IntegrationTests;

public sealed class MapImageProcessorTests : IDisposable
{
	private readonly string root = Path.Combine(Path.GetTempPath(), $"map-codecs-{Guid.NewGuid():N}");
	private readonly VipsMapImageProcessor processor = new();

	public MapImageProcessorTests() => Directory.CreateDirectory(root);

	[Theory]
	[InlineData(23, 17)]
	[InlineData(1025, 513)]
	public async Task NativePyramidKeepsOrientationPixelsAlphaAndGeometry(int width, int height)
	{
		processor.CheckCapabilities();
		var pixels = new byte[width * height * 4];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var offset = (y * width + x) * 4;
				pixels[offset] = x < width / 2 ? (byte)200 : (byte)10;
				pixels[offset + 1] = y < height / 2 ? (byte)50 : (byte)220;
				pixels[offset + 2] = 100;
				pixels[offset + 3] = x >= width / 2 && y >= height / 2 ? (byte)0 : (byte)255;
			}
		}

		using var image = Image.NewFromMemory(pixels, width, height, 4, Enums.BandFormat.Uchar);
		var path = Path.Combine(root, "original");

		image.Pngsave(path);
		var geometry = MapGeometry.Create(width, height).Value!;
		var output = Path.Combine(root, "tiles");

		await processor.GenerateAsync(path, output, geometry, new Progress<int>(), CancellationToken.None);

		for (var zoom = 0; zoom <= geometry.MaxZoom; zoom++)
		{
			for (var x = 0; x < geometry.Columns(zoom); x++)
			{
				for (var y = 0; y < geometry.Rows(zoom); y++)
				{
					// The processor normalizes libvips output to public zoom / column / row paths.
					using var encoded = Image.NewFromBuffer(File.ReadAllBytes(Path.Combine(output,
						$"{zoom}", $"{x}", $"{y}.webp")));
					using var tile = encoded.HasAlpha() ? encoded.Copy() : encoded.Bandjoin(255);

					Assert.Equal(512, tile.Width);
					Assert.Equal(512, tile.Height);
					Assert.True(tile.HasAlpha());

					if ((x + 1) * 512 * geometry.Resolutions.ElementAt(zoom) > width ||
						(y + 1) * 512 * geometry.Resolutions.ElementAt(zoom) > height)
						Assert.Equal(0d, tile[511, 511][3]);

					if (x == 0 && y == 0)
						Assert.Equal([200d, 50d, 100d, 255d], tile[0, 0]);

					if (zoom == geometry.MaxZoom)
					{
						var sourceX = x * 512;
						var sourceY = y * 512;
						var expected = pixels.AsSpan((sourceY * width + sourceX) * 4, 4).ToArray();
						var actual = tile[0, 0];

						Assert.Equal((double)expected[3], actual[3]);

						if (expected[3] != 0)
							Assert.Equal(expected.Select(value => (double)value), actual);
					}
				}
			}
		}
	}

	[Fact]
	public async Task CancellationStopsNativeWork()
	{
		using var cancellation = new CancellationTokenSource();

		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processor.GenerateAsync("unused", root,
			MapGeometry.Create(1, 1).Value!, new Progress<int>(), cancellation.Token));
	}

	[Fact]
	public async Task NativeProgressCanCancelAnEvaluatingPyramid()
	{
		using var image = Image.Black(4097, 2049, bands: 3);
		var path = Path.Combine(root, "large.png");

		image.Pngsave(path);
		using var cancellation = new CancellationTokenSource();
		var progress = new CancelingProgress(cancellation);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processor.GenerateAsync(path,
			Path.Combine(root, "canceled"), MapGeometry.Create(4097, 2049).Value!, progress, cancellation.Token));
		Assert.True(progress.Reports > 0);
	}

	private sealed class CancelingProgress(CancellationTokenSource cancellation) : IProgress<int>
	{
		public int Reports { get; private set; }

		public void Report(int value)
		{
			Reports++;

			if (value > 0)
				cancellation.Cancel();
		}
	}

	public void Dispose() => Directory.Delete(root, true);
}
