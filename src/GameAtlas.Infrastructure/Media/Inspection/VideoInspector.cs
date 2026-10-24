using GameAtlas.Application.Media.Inspection;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Result;
using MediaInfo.Analysis;
using MediaInfo.Analysis.Native;
using MediaInfo.Analysis.Results;
using MediaInfo.Analysis.Sources;
using Microsoft.Extensions.Options;

namespace GameAtlas.Infrastructure.Media.Inspection;

public sealed class VideoInspector(IOptions<MediaLimits> options) : IDisposable
{
	private readonly SemaphoreSlim concurrency = new(options.Value.ConcurrentVideoInspections);
	private readonly IMediaInfoAnalyzer analyzer = MediaInfoAnalyzer.CreateDefault();

	public async Task<Result<MediaInspection>> InspectAsync(string path, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var contentType = InspectContainer(path);

		if (contentType is null)
			return MediaErrors.InvalidContent;

		await concurrency.WaitAsync(cancellationToken);

		try
		{
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.ProbeTimeoutSeconds));
			await using var original = File.OpenRead(path);
			using var bounded = new InspectionReadStream(original, options.Value.VideoInspectionBytes);
			var result = await analyzer.AnalyzeAsync(new StreamMediaSource(bounded, leaveOpen: true,
				name: "uploaded-video"), timeout.Token);

			cancellationToken.ThrowIfCancellationRequested();

			if (bounded.BudgetExceeded || timeout.IsCancellationRequested)
				return MediaErrors.InvalidContent;

			if (result.Failure?.Reason == AnalysisFailureReason.NativeLibraryUnavailable)
				throw new InvalidOperationException("The packaged MediaInfo native library is unavailable.");

			if (result.Failure?.Reason == AnalysisFailureReason.Unknown)
				throw new InvalidOperationException("MediaInfo encountered an unexpected inspection failure.");

			if (!result.Success ||
				result.BestVideoStream is not { Width: > 0, Height: > 0 })
				return MediaErrors.InvalidContent;

			var valid = contentType == "video/mp4"
				? result.General.Format == "MPEG-4"
				: result.General.Format == "WebM";

			return valid ? new MediaInspection(contentType) : MediaErrors.InvalidContent;
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			return MediaErrors.InvalidContent;
		}
		catch (InvalidDataException)
		{
			return MediaErrors.InvalidContent;
		}
		finally
		{
			concurrency.Release();
		}
	}

	public void Dispose() => concurrency.Dispose();

	public void CheckCapabilities()
	{
		using var handle = MediaInfoLibFactory.Instance.Create();

		if (!handle.IsAvailable)
			throw new InvalidOperationException("Media inspection requires the packaged MediaInfo native library.");
	}

	private static string? InspectContainer(string path)
	{
		using var stream = File.OpenRead(path);
		Span<byte> header = stackalloc byte[12];

		if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) != header.Length)
			return null;

		if (header[4..8].SequenceEqual("ftyp"u8))
		{
			var brand = System.Text.Encoding.ASCII.GetString(header[8..]);

			return brand is "isom" or "iso2" or "mp41" or "mp42" or "avc1" or "dash" or "M4V "
				? "video/mp4" : null;
		}

		// MediaInfo verifies WebM DocType; the EBML signature alone also matches Matroska.
		return header[..4].SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 }) ? "video/webm" : null;
	}
}
