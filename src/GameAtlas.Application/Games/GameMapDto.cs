using GameAtlas.Application.Media;

namespace GameAtlas.Application.Games;

public sealed record GameMapDto(Guid Id, string Name, ImageAssetDto? Preview);
