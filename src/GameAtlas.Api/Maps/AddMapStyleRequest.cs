namespace GameAtlas.Api.Maps;

public sealed record AddMapStyleRequest(string? Name, string? BackgroundColor, Guid? SourceAssetId);
