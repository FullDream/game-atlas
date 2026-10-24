using System.Text.Json;
using GameAtlas.Application.Maps.Images;
using GameAtlas.Domain.Maps;
using NetVips;

namespace GameAtlas.Infrastructure.Maps.Images;

public sealed class VipsMapImageProcessor : IMapImageProcessor
{
	static VipsMapImageProcessor() => Cache.Max = 0;

	public async Task GenerateAsync(string originalPath, string outputDirectory, MapGeometry geometry,
		IProgress<int> progress, CancellationToken cancellationToken)
	{
		try
		{
			await Task.Run(() => Generate(originalPath, outputDirectory, geometry, progress, cancellationToken),
				cancellationToken);
		}
		catch (VipsException) when (cancellationToken.IsCancellationRequested)
		{
			throw new OperationCanceledException(cancellationToken);
		}
	}

	private static void Generate(string originalPath, string outputDirectory, MapGeometry geometry,
		IProgress<int> progress, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		Directory.CreateDirectory(outputDirectory);
		using var original = Image.NewFromFile(originalPath, access: Enums.Access.Sequential,
			failOn: Enums.FailOn.Error);

		if (original.Width != geometry.Width || original.Height != geometry.Height)
			throw new InvalidDataException("The source dimensions no longer match the map.");

		using var color = original.Colourspace(Enums.Interpretation.Srgb);
		using var pixels = color.Cast(Enums.BandFormat.Uchar);
		using var rgba = pixels.HasAlpha() ? pixels.Copy() : pixels.Bandjoin(255);
		var nativeOutput = Path.Combine(outputDirectory, "native");

		rgba.SetProgress(progress, cancellationToken);

		rgba.Dzsave(nativeOutput, layout: Enums.ForeignDzLayout.Google, suffix: ".webp[lossless]",
			tileSize: MapGeometry.TileSize, overlap: 0, centre: false, depth: Enums.ForeignDzDepth.Onetile,
			background: [0, 0, 0, 0], skipBlanks: -1);

		cancellationToken.ThrowIfCancellationRequested();
		ArrangeTiles(nativeOutput, outputDirectory, geometry, cancellationToken);
		File.WriteAllText(Path.Combine(outputDirectory, "manifest.json"), JsonSerializer.Serialize(new
		{
			geometry.Width,
			geometry.Height,
			geometry.MaxZoom,
			TileSize = MapGeometry.TileSize
		}));
	}

	private static void ArrangeTiles(string nativeOutput, string output, MapGeometry geometry,
		CancellationToken cancellationToken)
	{
		for (var zoom = 0; zoom <= geometry.MaxZoom; zoom++)
		{
			for (var x = 0; x < geometry.Columns(zoom); x++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var column = Path.Combine(output, $"{zoom}", $"{x}");

				Directory.CreateDirectory(column);

				for (var y = 0; y < geometry.Rows(zoom); y++)
					File.Move(Path.Combine(nativeOutput, $"{zoom}", $"{y}", $"{x}.webp"),
						Path.Combine(column, $"{y}.webp"));
			}
		}

		Directory.Delete(nativeOutput, recursive: true);
	}

	public void CheckCapabilities()
	{
		try
		{
			using var fixture = Image.Black(1, 1, bands: 4);
			using var png = Image.NewFromBuffer(fixture.PngsaveBuffer());
			using var jpeg = Image.NewFromBuffer(fixture.JpegsaveBuffer());
			using var webp = Image.NewFromBuffer(fixture.WebpsaveBuffer(lossless: true));
			using var avif = Image.NewFromBuffer(fixture.HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Av1));

			if (png.Width != 1 || jpeg.Width != 1 || webp.Width != 1 || avif.Width != 1)
				throw new InvalidOperationException("The image codecs failed their startup check.");
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException("Map processing requires compatible libvips with PNG/JPEG/WebP/AVIF codecs.",
				exception);
		}
	}

}
