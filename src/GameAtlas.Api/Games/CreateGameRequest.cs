namespace GameAtlas.Api.Games;

public sealed record CreateGameRequest(string? Name, string? Slug, Guid? ImageAssetId);
