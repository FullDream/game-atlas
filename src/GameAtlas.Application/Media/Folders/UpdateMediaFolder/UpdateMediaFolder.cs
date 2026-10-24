using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.Folders.UpdateMediaFolder;

public sealed record UpdateMediaFolder(Guid Id, Optional<string?> Name = default, Optional<Guid?> ParentId = default)
	: ICommand<Result<MediaFolderDto>>;
