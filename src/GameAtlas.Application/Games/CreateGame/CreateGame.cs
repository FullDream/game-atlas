using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Games.CreateGame;

public sealed record CreateGame(string? Name, string? Slug, Guid? ImageAssetId)
	: ICommand<Result<GameDto>>;
