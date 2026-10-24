using GameAtlas.Domain.Result;

namespace GameAtlas.Infrastructure.Maps.Persistence;

public static class MapOwnershipErrors
{
	public static Error Invalid(string message) => new("Map.Ownership.Preparation.Invalid", message,
		Type: ErrorType.Validation);
}
