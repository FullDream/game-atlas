namespace GameAtlas.Application.Common.Pagination;

public sealed record PaginatedResult<T>(IReadOnlyCollection<T> Items, int Limit, int Offset, int Total);
