using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Maps.DeleteMapStyle;

public sealed record DeleteMapStyle(Guid GameId, Guid MapId, Guid StyleId) : ICommand<Result>;
