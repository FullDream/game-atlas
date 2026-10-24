using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.CreateCategory;

public sealed record CreateCategory(
	Guid GameId,
	Guid MapId, Guid GroupId, string? Name, string? Icon, string? Color, Optional<int?> Order = default)
	: ICommand<Result<CategoryDto>>;
