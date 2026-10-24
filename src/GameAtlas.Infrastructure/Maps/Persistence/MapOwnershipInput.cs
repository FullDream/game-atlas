namespace GameAtlas.Infrastructure.Maps.Persistence;

public sealed record MapOwnershipInput(IReadOnlyCollection<MapOwnershipMap>? Maps,
	IReadOnlyCollection<MapOwnershipGroup>? Groups);
