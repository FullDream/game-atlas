using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.Folders.GetMediaFolder;

public sealed record GetMediaFolder(Guid Id) : IQuery<Result<MediaFolderDetailsDto>>;
