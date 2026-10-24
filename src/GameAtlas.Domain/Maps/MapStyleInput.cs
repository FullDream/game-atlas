namespace GameAtlas.Domain.Maps;

public sealed record MapStyleInput(string? Name, string? BackgroundColor, string Format, int Width, int Height,
	long ByteLength);
