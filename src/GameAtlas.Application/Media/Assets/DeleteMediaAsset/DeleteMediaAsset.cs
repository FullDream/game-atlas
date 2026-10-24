using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.Assets.DeleteMediaAsset;

public sealed record DeleteMediaAsset(Guid Id) : ICommand<Result>;
