using GameAtlas.Domain;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.UpdateGame;

public sealed record UpdateGame(
	Guid Id,
	Optional<string?> Name = default,
	Optional<string?> Slug = default,
	Optional<string?> Image = default,
	Optional<Guid?> MapId = default,
	Optional<GameStatus?> Status = default) : ICommand<Result<GameDto>>;
