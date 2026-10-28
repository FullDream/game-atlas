using GameAtlas.Api.Http;
using GameAtlas.Api.Media.Uploads;
using GameAtlas.Api.Results;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Media.Assets.DeleteMediaAsset;
using GameAtlas.Application.Media.Folders.DeleteMediaFolder;
using GameAtlas.Application.Media.Folders.GetMediaFolder;
using GameAtlas.Application.Media.ListMedia;
using GameAtlas.Application.Media;
using Mediator;

namespace GameAtlas.Api.Media;

public static class Endpoints
{
	public static RouteGroupBuilder MapMedia(this RouteGroupBuilder group)
	{
		var media = group.MapGroup("/media").WithTags("Media");

		media.MapGet("", ListAsync).WithName("ListMedia").WithSummary("List media");
		media.MapGet("/folders/{id}", GetFolderAsync).WithName("GetMediaFolder").WithSummary("Get media folder");
		media.MapPost("/folders", CreateFolderAsync).WithName("CreateMediaFolder").WithSummary("Create media folder");
		media.MapPost("/assets", UploadAsync).WithName("UploadMediaAsset").WithSummary("Upload media asset")
			.Accepts<UploadMediaAssetRequest>("multipart/form-data")
			.Produces<MediaAssetDto>()
			.ProducesProblem(StatusCodes.Status400BadRequest)
			.ProducesProblem(StatusCodes.Status404NotFound)
			.ProducesProblem(StatusCodes.Status413PayloadTooLarge)
			.ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
			.ProducesProblem(StatusCodes.Status500InternalServerError)
			.DisableAntiforgery();

		media.MapPatch("/folders/{id}", UpdateFolderAsync).WithName("UpdateMediaFolder")
			.WithSummary("Update media folder").WithMergePatch<UpdateMediaFolderRequest>();

		media.MapPatch("/assets/{id}", UpdateAssetAsync).WithName("UpdateMediaAsset")
			.WithSummary("Update media asset").WithMergePatch<UpdateMediaAssetRequest>();

		media.MapDelete("/folders/{id}", DeleteFolderAsync).WithName("DeleteMediaFolder")
			.WithSummary("Delete media folder");

		media.MapDelete("/assets/{id}", DeleteAssetAsync).WithName("DeleteMediaAsset")
			.WithSummary("Delete media asset");

		return media;
	}

	private static async Task<OutcomeResult<MediaFolderDetailsDto>> GetFolderAsync(Guid id, IMediator mediator,
		CancellationToken cancellationToken) => await mediator.Send(new GetMediaFolder(id), cancellationToken);

	private static async Task<OutcomeResult> DeleteAssetAsync(Guid id, IMediator mediator,
		CancellationToken cancellationToken) => await mediator.Send(new DeleteMediaAsset(id), cancellationToken);

	private static Task<IResult> UploadAsync(HttpRequest request, MediaUploadWorkflow workflow,
		CancellationToken cancellationToken) => workflow.UploadAsync(request, cancellationToken);

	private static async Task<OutcomeResult<PaginatedResult<MediaItemDto>>> ListAsync(
		[AsParameters] ListMediaRequest request, IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToQuery(), cancellationToken);

	private static async Task<OutcomeResult<MediaFolderDto>> CreateFolderAsync(CreateMediaFolderRequest request,
		IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(), cancellationToken);

	private static async Task<OutcomeResult<MediaAssetDto>> UpdateAssetAsync(Guid id, UpdateMediaAssetRequest request,
		IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(id), cancellationToken);

	private static async Task<OutcomeResult<MediaFolderDto>> UpdateFolderAsync(Guid id, UpdateMediaFolderRequest request,
		IMediator mediator, CancellationToken cancellationToken) =>
		await mediator.Send(request.ToCommand(id), cancellationToken);

	private static async Task<OutcomeResult> DeleteFolderAsync(Guid id, IMediator mediator,
		CancellationToken cancellationToken) => await mediator.Send(new DeleteMediaFolder(id), cancellationToken);
}
