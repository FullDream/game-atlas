using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Markers.UpdateMarker;

public sealed record UpdateMarker(Guid GameId, Guid MapId, Guid Id, Optional<Guid?> CategoryId = default,
	Optional<string?> Name = default, Optional<string?> Description = default,
	Optional<PositionPatch?> Position = default, Optional<IReadOnlyCollection<Guid?>?> Media = default)
	: ICommand<Result<MarkerDto>>;
