using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.GetGame;

public sealed record GetGame(Guid Id) : IQuery<Result<GameDto>>;
