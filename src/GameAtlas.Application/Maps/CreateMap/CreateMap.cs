using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.CreateMap;

public sealed record CreateMap(Guid GameId, string? Name, int? Width, int? Height) : ICommand<Result<MapDto>>;
