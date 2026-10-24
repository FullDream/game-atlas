using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.Folders.DeleteMediaFolder;

public sealed record DeleteMediaFolder(Guid Id) : ICommand<Result>;
