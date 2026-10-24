using System.Diagnostics;
using System.Text.Json;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Images;
using NetVips;

namespace GameAtlas.IntegrationTests;

public class MapResourceTests
{
	[Fact]
	[Trait("Category", "NativeResources")]
	public async Task MaximumDimensionFixtureProducesEveryTileWithObservedNativeResources()
	{
		var root = Path.Combine(Path.GetTempPath(), $"map-resource-{Guid.NewGuid():N}");

		Directory.CreateDirectory(root);

		try
		{
			var processor = new VipsMapImageProcessor();
			var original = Path.Combine(root, "original.png");
			var output = Path.Combine(root, "tiles");
			using var image = Image.Black(16384, 16384, bands: 3);

			image.Pngsave(original);
			var watch = Stopwatch.StartNew();

			await processor.GenerateAsync(original, output, MapGeometry.Create(16384, 16384).Value!,
				new Progress<int>(), CancellationToken.None);
			watch.Stop();
			var tiles = Directory.GetFiles(output, "*.webp", SearchOption.AllDirectories);

			Assert.Equal(1365, tiles.Length);
			using var last = Image.NewFromBuffer(
				await File.ReadAllBytesAsync(Path.Combine(output, "5", "31", "31.webp")));

			Assert.Equal(512, last.Width);
			Assert.Equal(512, last.Height);
			using var process = Process.GetCurrentProcess();
			var observation = new
			{
				Width = 16384,
				Height = 16384,
				Tiles = tiles.Length,
				ElapsedSeconds = watch.Elapsed.TotalSeconds,
				PeakWorkingSetBytes = process.PeakWorkingSet64,
				OriginalBytes = new FileInfo(original).Length,
				TileBytes = tiles.Sum(path => new FileInfo(path).Length),
				Runtime = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier
			};

			await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), "ga-map-resource-observation.json"),
				JsonSerializer.Serialize(observation));
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}
}
