using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Media;
using GameAtlas.Application.Media.Storage;

namespace GameAtlas.IntegrationTests;

public sealed class MediaByteLimitTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ActualDefaultBoundaryStreamsWithoutContentLengthAndRejectsOneExtraByte(bool exceed)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var maximum = new MediaLimits().MaximumBytes;
		using var source = new PaddedImageStream(MapTestData.ImageBytes(), maximum + (exceed ? 1 : 0));
		using var body = new MultipartFormDataContent();

		body.Add(new StreamContent(source), "file", "original.png");
		using var response = await client.PostAsync("/media/assets", body);

		Assert.Equal(exceed ? HttpStatusCode.RequestEntityTooLarge : HttpStatusCode.OK, response.StatusCode);
		Assert.Empty(Directory.GetFiles(Path.Combine(factory.MediaStoragePath, "staging")));

		if (!exceed)
		{
			var asset = Assert.IsType<ImageAssetDto>(await response.Content.ReadFromJsonAsync<MediaAssetDto>());

			Assert.Equal(maximum, asset.ByteLength);
			Assert.Equal(maximum, new FileInfo(Path.Combine(factory.MediaStoragePath, "assets", asset.Id.ToString("N"))).Length);
		}
		else
		{
			Assert.Empty(Directory.GetFiles(factory.MediaStoragePath, "*", SearchOption.AllDirectories));
		}
	}

	private sealed class PaddedImageStream(byte[] prefix, long length) : Stream
	{
		private long position;

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => position;
			set => throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

		public override int Read(Span<byte> buffer)
		{
			var count = (int)Math.Min(buffer.Length, length - position);

			buffer[..count].Clear();

			if (position < prefix.Length)
				prefix.AsSpan((int)position, Math.Min(count, prefix.Length - (int)position)).CopyTo(buffer);

			position += count;

			return count;
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			return ValueTask.FromResult(Read(buffer.Span));
		}

		public override void Flush() => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}
}
