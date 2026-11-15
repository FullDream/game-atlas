using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.GetPublicGame;

public sealed record GetPublicGame(string Slug) : IQuery<Result<PublicGameDto>>;
