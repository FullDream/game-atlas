using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.Folders.CreateMediaFolder;

public sealed record CreateMediaFolder(string? Name, Guid? ParentId = null) : ICommand<Result<MediaFolderDto>>;
