using GameAtlas.Application.Media.Inspection;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;

namespace GameAtlas.Infrastructure.Media.Inspection;

public sealed class MediaInspector(RasterInspector raster, SvgInspector svg, VideoInspector video) : IMediaInspector
{
	public async Task<Result<MediaInspection>> InspectAsync(string path, CancellationToken cancellationToken)
	{
		var header = new byte[32];

		await using (var stream = File.OpenRead(path))
			await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

		if (RasterInspector.Detect(header) is not null)
			return await Task.Run(() => raster.Inspect(path, cancellationToken), cancellationToken);

		if (header.AsSpan(4, 4).SequenceEqual("ftyp"u8))
		{
			if (header.AsSpan(8, 4).SequenceEqual("qt  "u8))
				return MediaErrors.InvalidContent;

			return await video.InspectAsync(path, cancellationToken);
		}

		if (header.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 }))
			return await video.InspectAsync(path, cancellationToken);

		return await Task.Run(() => svg.Inspect(path, cancellationToken), cancellationToken);
	}
}
