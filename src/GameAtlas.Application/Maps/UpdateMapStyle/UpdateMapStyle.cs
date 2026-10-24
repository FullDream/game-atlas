using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.UpdateMapStyle;

public sealed record UpdateMapStyle(Guid GameId, Guid MapId, Guid StyleId, Optional<string?> Name = default,
	Optional<string?> BackgroundColor = default) : ICommand<Result<MapStyleDto>>;
