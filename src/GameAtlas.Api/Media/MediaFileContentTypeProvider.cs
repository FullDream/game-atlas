using GameAtlas.Application.Media.Storage;
using Microsoft.AspNetCore.StaticFiles;

namespace GameAtlas.Api.Media;

internal sealed class MediaFileContentTypeProvider(IMediaStorage storage) : IContentTypeProvider
{
	public bool TryGetContentType(string subpath, out string contentType)
	{
		contentType = "application/octet-stream";

		if (subpath.Length < 2 || !Guid.TryParseExact(subpath.AsSpan(1), "N", out var id))
			return false;

		var path = storage.AssetPath(id);

		if (!File.Exists(path))
			return false;

		Span<byte> header = stackalloc byte[32];
		using var stream = File.OpenRead(path);
		var length = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

		contentType = Detect(header[..length]);

		return true;
	}

	private static string Detect(ReadOnlySpan<byte> signature)
	{
		if (signature.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
			return "image/png";

		if (signature.StartsWith(new byte[] { 255, 216, 255 }))
			return "image/jpeg";

		if (signature.Length >= 12)
		{
			if (signature[..4].SequenceEqual("RIFF"u8) && signature[8..12].SequenceEqual("WEBP"u8))
				return "image/webp";

			if (signature[4..8].SequenceEqual("ftyp"u8))
				return signature[8..12].SequenceEqual("avif"u8) ? "image/avif" : "video/mp4";
		}

		if (signature.StartsWith(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 }))
			return "video/webm";

		// Only inspected originals are promoted into this directory. SVG has no fixed binary signature.
		return "image/svg+xml";
	}
}
