namespace GameAtlas.Application.Markers.Media.Video;

public sealed record VideoMediaDto(string Url, string? Preview) : MarkerMediaDto;
