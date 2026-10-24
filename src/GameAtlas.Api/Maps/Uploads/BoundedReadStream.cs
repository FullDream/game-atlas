namespace GameAtlas.Api.Maps.Uploads;

internal sealed class BoundedReadStream(Stream inner, long maximumBytes) : Stream
{
	private long consumed;

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => consumed;
		set => throw new NotSupportedException();
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		var count = await inner.ReadAsync(buffer, cancellationToken);

		consumed += count;

		if (consumed > maximumBytes)
			throw new BadHttpRequestException("The multipart request exceeds its bounded envelope.", 413);

		return count;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

	public override int Read(byte[] buffer, int offset, int count)
	{
		var read = inner.Read(buffer, offset, count);

		consumed += read;

		if (consumed > maximumBytes)
			throw new BadHttpRequestException("The multipart request exceeds its bounded envelope.", 413);

		return read;
	}

	public override void Flush() => throw new NotSupportedException();
	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
