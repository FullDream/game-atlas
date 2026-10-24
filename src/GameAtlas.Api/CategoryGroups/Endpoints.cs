using GameAtlas.Api.Http;
using GameAtlas.Api.Results;
using GameAtlas.Application.CategoryGroups;
using GameAtlas.Application.CategoryGroups.DeleteCategoryGroup;
using GameAtlas.Application.CategoryGroups.ListCategoryGroups;
using Mediator;

namespace GameAtlas.Api.CategoryGroups;

public static class Endpoints
{
	public static RouteGroupBuilder MapCategoryGroups(this RouteGroupBuilder group)
	{
		var groups = group.MapGroup("/games/{gameId}/maps/{mapId}/category-groups").WithTags("Category groups");

		groups.MapGet("", ListCategoryGroupsAsync)
			.WithName("ListCategoryGroups").WithSummary("List category groups")
			.WithDescription("Returns all groups ordered by order, createdAt, and id, without pagination.");

		groups.MapPost("", CreateCategoryGroupAsync)
			.WithName("CreateCategoryGroup").WithSummary("Create category group")
			.WithDescription("Creates a group with an optional positive integer order; omission appends to the list. " +
				"Names must be unique within the game, " +
				"ignoring letter case and surrounding whitespace. Returns the complete stored group.");

		groups.MapPatch("/{id}", UpdateCategoryGroupAsync)
			.WithName("UpdateCategoryGroup").WithSummary("Update category group")
			.WithDescription("Supply name, positive integer order, or both. Duplicate orders are allowed. " +
				"Empty patches, null values, and read-only fields are rejected. " +
				"Both media types use JSON Merge Patch semantics. Unchanged values preserve updatedAt.")
			.WithMergePatch<UpdateCategoryGroupRequest>();

		groups.MapDelete("/{id}", DeleteCategoryGroupAsync)
			.WithName("DeleteCategoryGroup").WithSummary("Delete category group")
			.WithDescription("Permanently deletes the group. Returns 204 without a body; missing groups return 404. " +
				"Other groups retain their positions and timestamps, and the deleted name becomes reusable.");

		return groups;
	}

	private static async Task<OutcomeResult<IReadOnlyCollection<CategoryGroupDto>>> ListCategoryGroupsAsync(
		Guid gameId, Guid mapId, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new ListCategoryGroups(gameId, mapId), cancellationToken);

	private static async Task<OutcomeResult<CategoryGroupDto>> CreateCategoryGroupAsync(
		Guid gameId, Guid mapId, CreateCategoryGroupRequest request, IMediator mediator,
			CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId, mapId), cancellationToken);

	private static async Task<OutcomeResult<CategoryGroupDto>> UpdateCategoryGroupAsync(
		Guid gameId, Guid mapId, Guid id, UpdateCategoryGroupRequest request, IMediator mediator,
			CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId, mapId, id), cancellationToken);

	private static async Task<OutcomeResult> DeleteCategoryGroupAsync(
		Guid gameId, Guid mapId, Guid id, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new DeleteCategoryGroup(gameId, mapId, id), cancellationToken);
}
