using GameAtlas.Application.Common.Pagination;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Media.ListMedia;

public sealed record ListMedia(Guid? FolderId = null, int Offset = 0, int Limit = 20, string? Search = null)
	: IQuery<Result<PaginatedResult<MediaItemDto>>>;
