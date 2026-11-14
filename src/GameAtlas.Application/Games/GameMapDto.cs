using GameAtlas.Application.Media;
using GameAtlas.Domain;

namespace GameAtlas.Application.Games;

public sealed record GameMapDto(Guid Id, string Name, string Slug, ImageAssetDto? Preview, PublicationStatus Status);
