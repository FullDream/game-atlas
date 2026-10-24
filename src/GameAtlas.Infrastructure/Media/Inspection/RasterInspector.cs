using System.Buffers.Binary;
using GameAtlas.Application.Media.Inspection;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using Microsoft.Extensions.Options;
using NetVips;

namespace GameAtlas.Infrastructure.Media.Inspection;

public sealed class RasterInspector(IOptions<MediaLimits> options)
{
	static RasterInspector() => Cache.Max = 0;

	public Result<MediaInspection> Inspect(string path, CancellationToken cancellationToken)
	{
		var signature = new byte[32];

		using (var stream = File.OpenRead(path))
			stream.ReadAtLeast(signature, signature.Length, throwOnEndOfStream: false);

		var contentType = Detect(signature);

		if (contentType is null || contentType == "image/png" && IsAnimatedPng(path))
			return MediaErrors.InvalidContent;

		try
		{
			using var image = Image.NewFromFile(path, access: Enums.Access.Sequential, failOn: Enums.FailOn.Error);
			var pages = image.GetTypeOf("n-pages") == 0 ? 1 : (int)image.Get("n-pages");
			var limits = options.Value;

			if (pages != 1 || image.Width <= 0 || image.Height <= 0 ||
				image.Width > limits.MaximumImageSide || image.Height > limits.MaximumImageSide)
				return MediaErrors.InvalidDimensions;

			cancellationToken.ThrowIfCancellationRequested();
			string? placeholder = null;

			if (image.Width > limits.PlaceholderSide || image.Height > limits.PlaceholderSide)
			{
				using var thumbnail = image.ThumbnailImage(limits.PlaceholderSide, height: limits.PlaceholderSide,
					size: Enums.Size.Down);
				using var color = thumbnail.Colourspace(Enums.Interpretation.Srgb);
				var bytes = color.WebpsaveBuffer(q: 35, keep: Enums.ForeignKeep.None);

				placeholder = "data:image/webp;base64," + Convert.ToBase64String(bytes);

				if (placeholder.Length > limits.MaximumPlaceholderBytes)
					return MediaErrors.InvalidContent;
			}
			else
			{
				// Force decoding even when a small original does not need a placeholder.
				image.Avg();
			}

			cancellationToken.ThrowIfCancellationRequested();

			return new MediaInspection(contentType, image.Width, image.Height, placeholder);
		}
		catch (VipsException)
		{
			return MediaErrors.InvalidContent;
		}
	}

	internal static string? Detect(ReadOnlySpan<byte> signature)
	{
		if (signature.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
			return "image/png";

		if (signature.StartsWith(new byte[] { 255, 216, 255 }))
			return "image/jpeg";

		if (signature.Length >= 16 && signature[..4].SequenceEqual("RIFF"u8) &&
			signature[8..12].SequenceEqual("WEBP"u8))
			return "image/webp";

		if (signature.Length >= 16 && signature[4..8].SequenceEqual("ftyp"u8) &&
			signature[8..12].SequenceEqual("avif"u8))
			return "image/avif";

		return null;
	}

	private static bool IsAnimatedPng(string path)
	{
		using var stream = File.OpenRead(path);
		var header = new byte[8];

		stream.Position = 8;

		while (stream.Length - stream.Position >= 12)
		{
			stream.ReadExactly(header);
			var length = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0, 4));

			if (header.AsSpan(4).SequenceEqual("acTL"u8))
				return true;

			if (header.AsSpan(4).SequenceEqual("IEND"u8))
				return false;

			if (length > stream.Length - stream.Position - 4)
				return false;

			stream.Seek(length + 4L, SeekOrigin.Current);
		}

		return false;
	}
}
