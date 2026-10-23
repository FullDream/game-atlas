namespace GameAtlas.Application.Markers.Media.Image;

public sealed record ImageMediaDto(string Url, int Width, int Height, string Alt) : MarkerMediaDto;
