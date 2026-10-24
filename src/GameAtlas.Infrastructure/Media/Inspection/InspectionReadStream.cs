namespace GameAtlas.Infrastructure.Media.Inspection;

internal sealed class InspectionReadStream(Stream source, long maximumBytes) : Stream
{
	private long consumed;

	public bool BudgetExceeded { get; private set; }

	public override bool CanRead => source.CanRead;
	public override bool CanSeek => source.CanSeek;
	public override bool CanWrite => false;
	public override long Length => source.Length;

	public override long Position
	{
		get => source.Position;
		set => source.Position = value;
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		var read = await source.ReadAsync(buffer[..ReadSize(buffer.Length)], cancellationToken);

		Count(read);

		return read;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

	public override int Read(byte[] buffer, int offset, int count)
	{
		var read = source.Read(buffer, offset, ReadSize(count));

		Count(read);

		return read;
	}

	private int ReadSize(int requested)
	{
		if (requested == 0 || source.Position >= source.Length)
			return 0;

		var remaining = maximumBytes - consumed;

		if (remaining <= 0)
		{
			BudgetExceeded = true;

			throw new InvalidDataException("Video inspection exceeded its cumulative read budget.");
		}

		return (int)Math.Min(Math.Min(requested, 65536), remaining);
	}

	private void Count(int bytes)
	{
		consumed += bytes;

		if (consumed > maximumBytes)
		{
			BudgetExceeded = true;
			throw new InvalidDataException("Video inspection exceeded its cumulative read budget.");
		}
	}

	public override long Seek(long offset, SeekOrigin origin) => source.Seek(offset, origin);

	public override void Flush() => throw new NotSupportedException();
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
