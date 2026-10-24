namespace GameAtlas.Infrastructure.Maps.Persistence;

public sealed record MapOwnershipMap(Guid Id, Guid GameId, string? Name, int Width, int Height);
