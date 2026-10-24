using GameAtlas.Application.Maps.Uploads;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.AddMapStyle;

public sealed record AddMapStyle(Guid GameId, Guid MapId, Guid SessionId, StagedMapUpload Upload)
	: ICommand<Result<MapDto>>;
