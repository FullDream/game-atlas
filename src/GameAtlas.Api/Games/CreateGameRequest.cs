namespace GameAtlas.Api.Games;

public sealed record CreateGameRequest(string? Name, string? Slug, Guid? CoverId, Guid? BannerId = null,
	DateOnly? ReleaseDate = null, string? Developer = null, string? Description = null);
