namespace GameAtlas.Infrastructure.Maps.Processing;

internal sealed class MapProgressBuffer : IProgress<int>
{
	private int value;

	public int Value => Volatile.Read(ref value);

	public void Report(int progress)
	{
		progress = Math.Clamp(progress, 0, 99);
		var current = Value;

		while (progress > current)
		{
			var observed = Interlocked.CompareExchange(ref value, progress, current);

			if (observed == current)
				break;

			current = observed;
		}
	}
}
