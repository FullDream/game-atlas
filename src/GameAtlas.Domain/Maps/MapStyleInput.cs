using GameAtlas.Domain.Media;

namespace GameAtlas.Domain.Maps;

public sealed record MapStyleInput(string? Name, string? BackgroundColor, ImageAsset? Source);
