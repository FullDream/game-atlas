using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentValidation;
using GameAtlas.Api.Categories;
using GameAtlas.Api.Categories.Groups;
using GameAtlas.Api.Errors;
using GameAtlas.Api.Games;
using GameAtlas.Api.Markers;
using GameAtlas.Api.OpenApi.Optional;
using GameAtlas.Api.OpenApi.Parameters;
using GameAtlas.Api.OpenApi.Schemas;
using GameAtlas.Api.OpenApi.Validation;
using GameAtlas.Api.Serialization;
using GameAtlas.Api.Serialization.Optional;
using GameAtlas.Application.Categories.CreateCategory;
using GameAtlas.Application.Categories.UpdateCategory;
using GameAtlas.Application.Categories.Groups.CreateCategoryGroup;
using GameAtlas.Application.Categories.Groups.UpdateCategoryGroup;
using GameAtlas.Application.Games.CreateGame;
using GameAtlas.Application.Games.ListGames;
using GameAtlas.Application.Games.UpdateGame;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Markers.CreateMarker;
using GameAtlas.Application.Markers.UpdateMarker;
using Microsoft.AspNetCore.Routing;

namespace GameAtlas.Api;

public static class DependencyInjection
{
	public static IServiceCollection AddApi(this IServiceCollection services)
	{
		services.AddProblemDetails();
		services.AddOpenApiValidationSource<Media.UpdateMediaAssetRequest,
			Application.Media.Assets.UpdateMediaAsset.UpdateMediaAsset>();
		services.AddOpenApiValidationSource<Media.ListMediaRequest, Application.Media.ListMedia.ListMedia>();
		services.AddOpenApiValidationSource<Media.CreateMediaFolderRequest,
			Application.Media.Folders.CreateMediaFolder.CreateMediaFolder>();
		services.AddOpenApiValidationSource<Media.UpdateMediaFolderRequest,
			Application.Media.Folders.UpdateMediaFolder.UpdateMediaFolder>();
		services.AddExceptionHandler<MediaFolderConflictExceptionHandler>();
		services.AddOpenApiValidationSource<Maps.CreateMapRequest, Application.Maps.CreateMap.CreateMap>();
		services.AddOpenApiValidationSource<Maps.AddMapStyleRequest, Application.Maps.AddMapStyle.AddMapStyle>();
		services.AddOpenApiValidationSource<Maps.UpdateMapStyleRequest,
			Application.Maps.UpdateMapStyle.UpdateMapStyle>();

		services.AddExceptionHandler<GameSlugConflictExceptionHandler>();
		services.AddExceptionHandler<MapConflictExceptionHandler>();
		services.AddExceptionHandler<CategoryGroupNameConflictExceptionHandler>();
		services.AddExceptionHandler<CategoryNameConflictExceptionHandler>();
		services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
		services.AddOpenApiValidationSource<CreateGameRequest, CreateGame>();
		services.AddOpenApiValidationSource<ListGamesRequest, ListGames>();
		services.AddOpenApiValidationSource<UpdateGameRequest, UpdateGame>();
		services.AddOpenApiValidationSource<CreateCategoryGroupRequest, CreateCategoryGroup>();
		services.AddOpenApiValidationSource<UpdateCategoryGroupRequest, UpdateCategoryGroup>();
		services.AddOpenApiValidationSource<CreateCategoryRequest, CreateCategory>();
		services.AddOpenApiValidationSource<UpdateCategoryRequest, UpdateCategory>();
		services.AddOpenApiValidationSource<CreateMarkerRequest, CreateMarker>();
		services.AddOpenApiValidationSource<UpdateMarkerRequest, UpdateMarker>();
		services.AddOpenApiValidationSource<PositionRequest, PositionInput>();
		services.AddOpenApiValidationSource<PositionPatchRequest, PositionPatch>();
		services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.AddOptionalSupport();


			options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
			options.SerializerOptions.Converters.Add(
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));

			options.SerializerOptions.DictionaryKeyPolicy = new SegmentedNamingPolicy(JsonNamingPolicy.CamelCase);
			options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
		});

		services.AddValidatorsFromAssemblyContaining<FluentValidationSchemaTransformer>(includeInternalTypes: true);
		services.AddOpenApiValidation();
		services.AddOpenApi(options =>
		{
			options.CreateSchemaReferenceId = PublicSchemaNames.CreateReferenceId;

			options.AddOptionalSupport();
			options.AddOperationTransformer<CamelCaseQueryParametersTransformer>();
			options.AddSchemaTransformer<FluentValidationSchemaTransformer>();
			options.AddDocumentTransformer<MediaSchemaTransformer>();
			options.AddDocumentTransformer<CategorySchemaTransformer>();
		});

		return services;
	}
}
