using GameAtlas.Domain;

namespace GameAtlas.Application.Markers;

public sealed record PositionPatch(Optional<double?> X = default, Optional<double?> Y = default);
