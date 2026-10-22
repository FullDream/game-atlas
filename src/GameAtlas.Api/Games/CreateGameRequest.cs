namespace GameAtlas.Api.Games;

public sealed record CreateGameRequest(string? Name, string? Slug, string? Image, Guid? MapId = null);
