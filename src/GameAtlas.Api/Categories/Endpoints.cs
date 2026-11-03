using GameAtlas.Api.Categories.Groups;
using GameAtlas.Api.Http;
using GameAtlas.Api.Results;
using GameAtlas.Application.Categories;
using GameAtlas.Application.Categories.DeleteCategory;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Application.Categories.Groups.DeleteCategoryGroup;
using GameAtlas.Application.Categories.Groups.ListCategoryGroups;
using Mediator;

namespace GameAtlas.Api.Categories;

public static class Endpoints
{
	public static RouteGroupBuilder MapCategories(this RouteGroupBuilder api)
	{
		var groups = api.MapGroup("/games/{gameId}/maps/{mapId}/category-groups").WithTags("Categories");
		groups.MapGet("", async (Guid gameId, Guid mapId, IMediator mediator, CancellationToken cancellationToken) =>
			(OutcomeResult<IReadOnlyCollection<CategoryGroupDto>>)await mediator.Send(new ListCategoryGroups(gameId, mapId), cancellationToken))
			.WithName("ListCategoryGroups").WithSummary("List category tree");

		groups.MapPost("", async (Guid gameId, Guid mapId, CreateCategoryGroupRequest request, IMediator mediator, CancellationToken cancellationToken) =>
			(OutcomeResult<CategoryGroupDto>)await mediator.Send(request.ToCommand(gameId, mapId), cancellationToken))
			.WithName("CreateCategoryGroup").WithSummary("Create category group");

		groups.MapPatch("/{id}", async (Guid gameId, Guid mapId, Guid id, UpdateCategoryGroupRequest request, IMediator mediator, CancellationToken cancellationToken) =>
			(OutcomeResult<CategoryGroupDto>)await mediator.Send(request.ToCommand(gameId, mapId, id), cancellationToken))
			.WithName("UpdateCategoryGroup").WithSummary("Update category group").WithMergePatch<UpdateCategoryGroupRequest>();

		groups.MapDelete("/{id}", async (Guid gameId, Guid mapId, Guid id, IMediator mediator, CancellationToken cancellationToken) =>
			(OutcomeResult)await mediator.Send(new DeleteCategoryGroup(gameId, mapId, id), cancellationToken))
			.WithName("DeleteCategoryGroup").WithSummary("Delete category group");

		var categories = groups.MapGroup("/{groupId}/categories");
		categories.MapPost("", async (Guid gameId, Guid mapId, Guid groupId, CreateCategoryRequest request, IMediator mediator, CancellationToken cancellationToken) =>
			(OutcomeResult<CategoryDto>)await mediator.Send(request.ToCommand(gameId, mapId, groupId), cancellationToken))
			.WithName("CreateCategory").WithSummary("Create category");

		categories.MapPatch("/{id}", async (Guid gameId, Guid mapId, Guid groupId, Guid id, UpdateCategoryRequest request, IMediator mediator, CancellationToken cancellationToken) =>
			(OutcomeResult<CategoryDto>)await mediator.Send(request.ToCommand(gameId, mapId, groupId, id), cancellationToken))
			.WithName("UpdateCategory").WithSummary("Update category").WithMergePatch<UpdateCategoryRequest>();

		categories.MapDelete("/{id}", async (Guid gameId, Guid mapId, Guid groupId, Guid id, IMediator mediator, CancellationToken cancellationToken) =>
			(OutcomeResult)await mediator.Send(new DeleteCategory(gameId, mapId, groupId, id), cancellationToken))
			.WithName("DeleteCategory").WithSummary("Delete category");

		return groups;
	}
}
