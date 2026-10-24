using System.Text;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Infrastructure.Media.Inspection;
using GameAtlas.Infrastructure.Media.Storage;
using Microsoft.Extensions.Options;
using NetVips;

namespace GameAtlas.IntegrationTests;

public sealed class MediaInspectionTests
{
	[Theory]
	[InlineData("webp")]
	[InlineData("avif")]
	public void ActualMultiframeRasterImagesAreRejected(string format)
	{
		using var black = Image.Black(8, 8, bands: 3);
		using var white = black.NewFromImage([255, 255, 255]);
		using var frames = black.Join(white, Enums.Direction.Vertical);
		var bytes = format == "webp"
			? frames.WebpsaveBuffer(pageHeight: 8)
			: frames.HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Av1, pageHeight: 8);
		var path = Path.Combine(Path.GetTempPath(), $"ga-animation-{Guid.NewGuid():N}");

		try
		{
			File.WriteAllBytes(path, bytes);
			using var decoded = Image.NewFromFile(path);

			Assert.Equal(2, (int)decoded.Get("n-pages"));
			Assert.True(new RasterInspector(Options.Create(new MediaLimits())).Inspect(path, CancellationToken.None).IsFailure);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void MediaConfigurationRejectsUnsafeOrUnboundedLimits()
	{
		var validator = new MediaOptionsValidation();

		Assert.True(validator.Validate(null, new MediaLimits()).Succeeded);
		Assert.True(validator.Validate(null, new MediaLimits { MaximumBytes = 0 }).Failed);
		Assert.True(validator.Validate(null, new MediaLimits { MaximumImageSide = 16385 }).Failed);
		Assert.True(validator.Validate(null, new MediaLimits { MaximumPlaceholderBytes = int.MaxValue }).Failed);
		Assert.True(validator.Validate(null, new MediaLimits { MaximumXmlDepth = 257 }).Failed);
		Assert.True(validator.Validate(null, new MediaLimits { MaximumXmlCharacters = 0 }).Failed);
		Assert.True(validator.Validate(null, new MediaLimits { VideoInspectionBytes = 0 }).Failed);
		Assert.True(validator.Validate(null, new MediaLimits { ConcurrentVideoInspections = 0 }).Failed);
		Assert.True(validator.Validate(null, new MediaStorageOptions { StorageRoot = " " }).Failed);
	}

	[Fact]
	public void MediaPathsRejectDirectoryLinksBeforeReadingOrDeletingExternalFiles()
	{
		var root = Path.Combine(Path.GetTempPath(), $"ga-media-links-{Guid.NewGuid():N}");
		var outside = Path.Combine(root, "outside");
		var media = Path.Combine(root, "media");
		var link = Path.Combine(media, "assets");
		var id = Guid.CreateVersion7();

		Directory.CreateDirectory(outside);
		Directory.CreateDirectory(media);
		File.WriteAllText(Path.Combine(outside, id.ToString("N")), "protected");

		try
		{
			if (OperatingSystem.IsWindows())
			{
				var start = new System.Diagnostics.ProcessStartInfo("cmd.exe")
				{
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true
				};

				foreach (var argument in new[] { "/c", "mklink", "/J", link, outside })
					start.ArgumentList.Add(argument);

				using var process = System.Diagnostics.Process.Start(start)!;

				process.WaitForExit();
				Assert.Equal(0, process.ExitCode);
			}
			else
			{
				Directory.CreateSymbolicLink(link, outside);
			}

			var storage = new LocalMediaStorage(Options.Create(new MediaStorageOptions { StorageRoot = media }));

			Assert.Throws<IOException>(() => storage.AssetPath(id));
			Assert.Throws<IOException>(() => storage.DeleteAsset(id));
			Assert.Equal("protected", File.ReadAllText(Path.Combine(outside, id.ToString("N"))));
		}
		finally
		{
			if (Directory.Exists(link))
				Directory.Delete(link);

			Directory.Delete(root, recursive: true);
		}
	}

	[Theory]
	[InlineData("depth")]
	[InlineData("elements")]
	[InlineData("attributes")]
	[InlineData("characters")]
	public void SvgBudgetsBoundDifferentDimensionsOfParsingWork(string budget)
	{
		var limits = new MediaLimits();
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><g><rect/></g></svg>";

		switch (budget)
		{
			case "depth": limits.MaximumXmlDepth = 1; break;
			case "elements": limits.MaximumXmlElements = 2; break;
			case "attributes": limits.MaximumXmlAttributes = 2; break;
			case "characters": limits.MaximumXmlCharacters = 32; break;
		}

		AssertSvg(svg, false, limits);
	}

	[Fact]
	public void PlaceholderOutputBudgetRejectsAdmissionWithoutChangingTheOriginal()
	{
		var path = Path.Combine(Path.GetTempPath(), $"ga-placeholder-budget-{Guid.NewGuid():N}");
		var bytes = MapTestData.ImageBytes(64, 48);

		try
		{
			File.WriteAllBytes(path, bytes);
			var inspector = new RasterInspector(Options.Create(new MediaLimits { MaximumPlaceholderBytes = 32 }));

			Assert.True(inspector.Inspect(path, CancellationToken.None).IsFailure);
			Assert.Equal(bytes, File.ReadAllBytes(path));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Theory]
	[InlineData("png", 32, 32, false)]
	[InlineData("png", 16, 12, false)]
	[InlineData("png", 33, 12, true)]
	[InlineData("png", 12, 33, true)]
	[InlineData("jpeg", 64, 48, true)]
	[InlineData("webp", 64, 48, true)]
	[InlineData("avif", 64, 48, true)]
	public void NativeFormatsHaveBoundedPlaceholdersWithoutChangingOriginals(
		string format, int width, int height, bool expectedPlaceholder)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ga-inspect-{Guid.NewGuid():N}");
		using var image = Image.Black(width, height, bands: 4);
		var bytes = format switch
		{
			"png" => image.PngsaveBuffer(),
			"jpeg" => image.JpegsaveBuffer(),
			"webp" => image.WebpsaveBuffer(),
			"avif" => image.HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Av1),
			_ => throw new ArgumentOutOfRangeException(nameof(format))
		};

		try
		{
			File.WriteAllBytes(path, bytes);
			var inspector = new RasterInspector(Options.Create(new MediaLimits()));
			var result = inspector.Inspect(path, CancellationToken.None);

			Assert.True(result.IsSuccess);
			Assert.Equal($"image/{format}", result.Value!.ContentType);
			Assert.Equal(width, result.Value.Width);
			Assert.Equal(height, result.Value.Height);
			Assert.Equal(bytes, File.ReadAllBytes(path));
			Assert.Equal(expectedPlaceholder, result.Value.Placeholder is not null);

			if (result.Value.Placeholder is { } placeholder)
			{
				Assert.StartsWith("data:image/webp;base64,", placeholder);
				Assert.InRange(placeholder.Length, 1, 8192);
				using var preview = Image.NewFromBuffer(Convert.FromBase64String(placeholder.Split(',')[1]));

				Assert.InRange(preview.Width, 1, 32);
				Assert.InRange(preview.Height, 1, 32);

				if (format != "jpeg")
					Assert.True(preview.HasAlpha());
			}
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Theory]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>")]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg' onload='alert(1)'/>")]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg'><foreignObject/></svg>")]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg'><style>path{fill:red}</style></svg>")]
	[InlineData("<!DOCTYPE svg [<!ENTITY a 'x'>]><svg xmlns='http://www.w3.org/2000/svg'>&a;</svg>")]
	[InlineData("<?xml-stylesheet href='http://localhost/x'?><svg xmlns='http://www.w3.org/2000/svg'/>")]
	[InlineData("<svg xmlns='https://attacker.invalid'/>")]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg'><image href='data:image/svg+xml,x'/></svg>")]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg'><path fill='url(https://attacker.invalid)'/></svg>")]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg'><path fill='url(#missing)'/></svg>")]
	public void SvgRejectsActiveAndExternalContent(string svg) => AssertSvg(svg, false);

	[Fact]
	public void SvgAllowsStaticShapesAndLocalGradientsWithoutRenderingOrPlaceholder() => AssertSvg(
		"<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48' viewBox='0 0 64 48'>" +
		"<defs><linearGradient id='paint'><stop offset='0%' stop-color='#fff'/></linearGradient>" +
		"<clipPath id='clip'><rect width='32' height='32'/></clipPath>" +
		"<mask id='mask'><circle cx='16' cy='16' r='8' fill='white'/></mask></defs>" +
		"<path d='M0 0 L32 32 Z' fill='url(#paint)' clip-path='url(#clip)' mask='url(#mask)'/></svg>", true);

	[Fact]
	public async Task StorageUsesGeneratedPathsAndCountsActualBytes()
	{
		var root = Path.Combine(Path.GetTempPath(), $"ga-media-store-{Guid.NewGuid():N}");
		var storage = new LocalMediaStorage(Options.Create(new MediaStorageOptions { StorageRoot = root }));
		var session = Guid.CreateVersion7();
		var asset = Guid.CreateVersion7();
		var bytes = Encoding.UTF8.GetBytes("original bytes");

		try
		{
			var result = await storage.WriteAsync(session, new MemoryStream(bytes), bytes.Length,
				CancellationToken.None);

			Assert.True(result.IsSuccess);
			storage.Promote(session, asset);
			Assert.Equal(bytes, await File.ReadAllBytesAsync(storage.AssetPath(asset)));
			Assert.False(File.Exists(storage.StagingPath(session)));
			Assert.Throws<ArgumentException>(() => storage.AssetPath(Guid.Empty));
			var excessive = await storage.WriteAsync(Guid.CreateVersion7(), new MemoryStream(bytes), bytes.Length - 1,
				CancellationToken.None);

			Assert.False(excessive.IsSuccess);
			storage.DeleteAsset(asset);
			storage.DeleteAsset(asset);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private static void AssertSvg(string svg, bool valid, MediaLimits? limits = null)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ga-svg-{Guid.NewGuid():N}");
		var bytes = Encoding.UTF8.GetBytes(svg);

		try
		{
			File.WriteAllBytes(path, bytes);
			var result = new SvgInspector(Options.Create(limits ?? new MediaLimits())).Inspect(path, CancellationToken.None);

			Assert.Equal(valid, result.IsSuccess);
			Assert.Equal(bytes, File.ReadAllBytes(path));

			if (valid)
				Assert.Null(result.Value!.Placeholder);
		}
		finally
		{
			File.Delete(path);
		}
	}
}
