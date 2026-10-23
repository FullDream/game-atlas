namespace GameAtlas.Application.Markers.Media.Image;

public sealed record ImageMediaInput(string? Url, int? Width, int? Height, string? Alt = "") : MarkerMediaInput;
