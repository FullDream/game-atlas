using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.Groups.UpdateCategoryGroup;

public sealed record UpdateCategoryGroup(
	Guid GameId,
	Guid MapId, Guid Id, Optional<string?> Name = default, Optional<Guid?> BeforeId = default, Optional<Guid?> IconAssetId = default, Optional<string?> Color = default)
	: ICommand<Result<CategoryGroupDto>>;
