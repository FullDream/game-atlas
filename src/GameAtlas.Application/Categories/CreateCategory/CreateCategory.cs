using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.CreateCategory;

public sealed record CreateCategory(
	Guid GameId,
	Guid MapId, Guid GroupId, string? Name, Guid? IconAssetId, string? Color, Optional<Guid?> BeforeId = default)
	: ICommand<Result<CategoryDto>>;
