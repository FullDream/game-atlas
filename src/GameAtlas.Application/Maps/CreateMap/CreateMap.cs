using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.CreateMap;

public sealed record CreateMap(Guid GameId, string? Name, string? Slug, Optional<PublicationStatus?> Status = default) : ICommand<Result<MapDto>>;
