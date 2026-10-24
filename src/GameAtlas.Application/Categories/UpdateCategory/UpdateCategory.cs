using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.UpdateCategory;

public sealed record UpdateCategory(
	Guid GameId,
	Guid MapId, Guid GroupId, Guid Id, Optional<string?> Name = default, Optional<Guid?> IconAssetId = default,
	Optional<string?> Color = default, Optional<int?> Order = default, Optional<Guid?> CategoryGroupId = default)
	: ICommand<Result<CategoryDto>>;
