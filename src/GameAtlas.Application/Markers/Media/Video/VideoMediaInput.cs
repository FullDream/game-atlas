namespace GameAtlas.Application.Markers.Media.Video;

public sealed record VideoMediaInput(string? Url, string? Preview = null) : MarkerMediaInput;
