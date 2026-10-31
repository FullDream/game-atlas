using GameAtlas.Domain;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.UpdateGame;

public sealed record UpdateGame(
	Guid Id,
	Optional<string?> Name = default,
	Optional<string?> Slug = default,
	Optional<Guid?> CoverId = default,
	Optional<GameStatus?> Status = default,
	Optional<Guid?> BannerId = default,
	Optional<DateOnly?> ReleaseDate = default,
	Optional<string?> Developer = default,
	Optional<string?> Description = default) : ICommand<Result<GameDto>>;
