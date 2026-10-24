using GameAtlas.Application.Maps;

namespace GameAtlas.Api.Maps.Events;

internal sealed record MapStreamSnapshot(Guid MapId, Guid? DefaultStyleId,
	IReadOnlyCollection<MapStyleDto> Styles);
