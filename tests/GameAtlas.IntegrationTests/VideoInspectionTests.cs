using System.Net;
using System.Net.Http.Json;
using System.Buffers.Binary;
using System.Text;
using GameAtlas.Application.Media;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Infrastructure.Media.Inspection;
using Microsoft.Extensions.Options;
using MediaInfo.Analysis;
using MediaInfo.Analysis.Sources;

namespace GameAtlas.IntegrationTests;

public sealed class VideoInspectionTests
{
	[Fact]
	public async Task InspectionReadsAreChunkedAndSeekingCannotResetTheCumulativeBudget()
	{
		using var original = new MemoryStream(new byte[200000]);
		using var bounded = new InspectionReadStream(original, 70000);
		var buffer = new byte[100000];

		Assert.Equal(65536, bounded.Read(buffer, 0, buffer.Length));
		bounded.Seek(0, SeekOrigin.Begin);
		Assert.Equal(4464, await bounded.ReadAsync(buffer.AsMemory()));
		Assert.Throws<InvalidDataException>(() => bounded.Read(buffer, 0, 1));
		Assert.True(bounded.BudgetExceeded);
		Assert.Equal(4464, original.Position);
	}

	[Fact]
	public async Task ExhaustedProbeBudgetAndCancelledCallsDoNotHoldTheOnlyConcurrencySlot()
	{
		using var inspector = new VideoInspector(Options.Create(new MediaLimits
		{
			VideoInspectionBytes = 1,
			ConcurrentVideoInspections = 1
		}));
		using var cancelled = new CancellationTokenSource();

		cancelled.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			inspector.InspectAsync(Fixture("mp4"), cancelled.Token));

		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var first = await inspector.InspectAsync(Fixture("mp4"), timeout.Token);
		var second = await inspector.InspectAsync(Fixture("webm"), timeout.Token);

		Assert.True(first.IsFailure);
		Assert.True(second.IsFailure);
	}

	[Theory]
	[InlineData("mp4")]
	[InlineData("webm")]
	public async Task RealVideoIsInspectedWithoutExternalExecutablesAndPreservesBytes(string format)
	{
		var path = Fixture(format);
		var bytes = await File.ReadAllBytesAsync(path);
		using var inspector = new VideoInspector(Options.Create(new MediaLimits()));
		var result = await inspector.InspectAsync(path, CancellationToken.None);

		Assert.True(result.IsSuccess);
		Assert.Equal($"video/{format}", result.Value!.ContentType);
		Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var upload = new MultipartFormDataContent();

		upload.Add(new ByteArrayContent(bytes), "file", "misleading.png");
		using var response = await client.PostAsync("/media/assets", upload);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var dto = Assert.IsType<VideoAssetDto>(await response.Content.ReadFromJsonAsync<MediaAssetDto>());

		Assert.Equal($"video/{format}", dto.ContentType);
		Assert.Null(dto.Preview);
		using var original = await client.GetAsync(dto.Url);

		Assert.Equal(dto.ContentType, original.Content.Headers.ContentType!.MediaType);
		Assert.Equal(bytes, await original.Content.ReadAsByteArrayAsync());
	}

	[Fact]
	public async Task CancellationAndReadBudgetsAreRespected()
	{
		using var inspector = new VideoInspector(Options.Create(new MediaLimits { VideoInspectionBytes = 1 }));
		var excessive = await inspector.InspectAsync(Fixture("mp4"), CancellationToken.None);

		Assert.True(excessive.IsFailure);
		using var cancelled = new CancellationTokenSource();

		await cancelled.CancelAsync();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			inspector.InspectAsync(Fixture("mp4"), cancelled.Token));
	}

	[Fact]
	public async Task AudioOnlyMp4AndTruncatedContainersAreRejected()
	{
		var bytes = await File.ReadAllBytesAsync(Fixture("mp4"));
		var movie = Assert.Single(Boxes(bytes, 0, bytes.Length), box => box.Type == "moov");
		var removed = 0;

		foreach (var track in Boxes(bytes, movie.Offset + 8, movie.Offset + movie.Size)
			.Where(box => box.Type == "trak"))
		{
			var media = Assert.Single(Boxes(bytes, track.Offset + 8, track.Offset + track.Size),
				box => box.Type == "mdia");
			var handler = Assert.Single(Boxes(bytes, media.Offset + 8, media.Offset + media.Size),
				box => box.Type == "hdlr");

			if (Encoding.ASCII.GetString(bytes, handler.Offset + 16, 4) != "vide")
				continue;

			Encoding.ASCII.GetBytes("free").CopyTo(bytes, track.Offset + 4);
			removed++;
		}

		Assert.True(removed > 0);
		var path = Path.Combine(Path.GetTempPath(), $"ga-audio-only-{Guid.NewGuid():N}");

		try
		{
			await File.WriteAllBytesAsync(path, bytes);
			await using var stream = File.OpenRead(path);
			var analysis = await MediaInfoAnalyzer.CreateDefault().AnalyzeAsync(
				new StreamMediaSource(stream, leaveOpen: true, name: "audio-only"), CancellationToken.None);

			Assert.True(analysis.HasAudio);
			Assert.False(analysis.HasVideo);
			using var inspector = new VideoInspector(Options.Create(new MediaLimits()));
			var audioOnly = await inspector.InspectAsync(path, CancellationToken.None);

			Assert.True(audioOnly.IsFailure);
		}
		finally
		{
			File.Delete(path);
		}

		try
		{
			await File.WriteAllBytesAsync(path, bytes[..32]);
			using var inspector = new VideoInspector(Options.Create(new MediaLimits()));
			var malformed = await inspector.InspectAsync(path, CancellationToken.None);

			Assert.True(malformed.IsFailure);
		}
		finally
		{
			File.Delete(path);
		}
	}

	private static IEnumerable<(int Offset, int Size, string Type)> Boxes(byte[] bytes, int offset, int end)
	{
		while (offset + 8 <= end)
		{
			var size = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4)));
			var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);

			Assert.InRange(size, 8, end - offset);

			yield return (offset, size, type);

			offset += size;
		}
	}

	private static string Fixture(string format) =>
		Path.Combine(AppContext.BaseDirectory, "Fixtures", "Media", $"flower.{format}");
}
