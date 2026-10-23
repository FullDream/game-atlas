using GameAtlas.Api.Http;
using GameAtlas.Api.Results;
using GameAtlas.Application.Categories;
using GameAtlas.Application.Categories.DeleteCategory;
using GameAtlas.Application.Categories.ListCategories;
using Mediator;

namespace GameAtlas.Api.Categories;

public static class Endpoints
{
	public static RouteGroupBuilder MapCategories(this RouteGroupBuilder group)
	{
		var categories = group.MapGroup("/games/{gameId}/category-groups/{groupId}/categories").WithTags("Categories");

		categories.MapGet("", ListCategoriesAsync)
			.WithName("ListCategories").WithSummary("List categories")
			.WithDescription("Returns all categories ordered by order, createdAt, and id, without pagination. " +
				"Each category has a read-only markersCount of 0 until markers are implemented.");

		categories.MapPost("", CreateCategoryAsync)
			.WithName("CreateCategory").WithSummary("Create category")
			.WithDescription("Creates a category with a group-unique name, icon reference, #RGB or #RRGGBB color, " +
				"and optional positive order. Omitted order appends to the group. " +
				"Image formats are not restricted; markersCount is 0.");

		categories.MapPatch("/{id}", UpdateCategoryAsync)
			.WithName("UpdateCategory").WithSummary("Update category")
			.WithDescription("Supply name, icon, color, order, or categoryGroupId. The route identifies the current " +
				"group; categoryGroupId transfers to a group of the same game. Omitted fields remain unchanged, " +
				"including order on transfer. Use the new group route after transfer. Nulls and empty patches are " +
				"rejected. Both media types use JSON Merge Patch semantics; no-ops preserve updatedAt.")
			.WithMergePatch<UpdateCategoryRequest>();

		categories.MapDelete("/{id}", DeleteCategoryAsync)
			.WithName("DeleteCategory").WithSummary("Delete category")
			.WithDescription("Permanently deletes the category, returning empty 204. Missing categories return 404. " +
				"Survivor positions and timestamps are preserved, and the deleted name becomes reusable.");

		return categories;
	}

	private static async Task<OutcomeResult<IReadOnlyCollection<CategoryDto>>> ListCategoriesAsync(
		Guid gameId, Guid groupId, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new ListCategories(gameId, groupId), cancellationToken);

	private static async Task<OutcomeResult<CategoryDto>> CreateCategoryAsync(
		Guid gameId, Guid groupId, CreateCategoryRequest request, IMediator mediator,
		CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId, groupId), cancellationToken);

	private static async Task<OutcomeResult<CategoryDto>> UpdateCategoryAsync(
		Guid gameId, Guid groupId, Guid id, UpdateCategoryRequest request, IMediator mediator,
		CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(gameId, groupId, id), cancellationToken);

	private static async Task<OutcomeResult> DeleteCategoryAsync(
		Guid gameId, Guid groupId, Guid id, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(new DeleteCategory(gameId, groupId, id), cancellationToken);
}
