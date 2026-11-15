using GameAtlas.Application.Media;

namespace GameAtlas.Application.Games;

public sealed record PublicGameMapDto(Guid Id, string Name, string Slug, ImageAssetDto? Preview);
