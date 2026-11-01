using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.CreateMap;

public sealed record CreateMap(Guid GameId, string? Name) : ICommand<Result<MapDto>>;
