using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.AddMapStyle;

public sealed record AddMapStyle(Guid GameId, Guid MapId, string? Name, string? BackgroundColor, Guid? SourceAssetId)
	: ICommand<Result<MapDto>>;
