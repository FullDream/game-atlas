using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.DeleteGame;

public sealed record DeleteGame(Guid Id) : ICommand<Result>;
