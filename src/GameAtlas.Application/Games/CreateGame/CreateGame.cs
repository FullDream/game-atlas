using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.CreateGame;

public sealed record CreateGame(string? Name, string? Slug, string? Image, Guid? MapId = null)
	: ICommand<Result<GameDto>>;
