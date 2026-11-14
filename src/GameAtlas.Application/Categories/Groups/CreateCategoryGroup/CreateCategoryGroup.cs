using GameAtlas.Domain;
using GameAtlas.Domain.Result;
using Mediator;

namespace GameAtlas.Application.Categories.Groups.CreateCategoryGroup;

public sealed record CreateCategoryGroup(
	Guid GameId,
	Guid MapId,
	string? Name,
	Optional<Guid?> BeforeId = default,
	Guid? IconAssetId = null,
	string? Color = "#F59E0B",
	Optional<PublicationStatus?> Status = default)
	: ICommand<Result<CategoryGroupDto>>;
