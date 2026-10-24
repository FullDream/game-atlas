using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentValidation;
using GameAtlas.Api.Categories;
using GameAtlas.Api.CategoryGroups;
using GameAtlas.Api.Errors;
using GameAtlas.Api.Games;
using GameAtlas.Api.Markers;
using GameAtlas.Api.Markers.Media;
using GameAtlas.Api.OpenApi.Optional;
using GameAtlas.Api.OpenApi.Parameters;
using GameAtlas.Api.OpenApi.Validation;
using GameAtlas.Api.Serialization;
using GameAtlas.Api.Serialization.Optional;
using GameAtlas.Application.Categories.CreateCategory;
using GameAtlas.Application.Categories.UpdateCategory;
using GameAtlas.Application.CategoryGroups.CreateCategoryGroup;
using GameAtlas.Application.CategoryGroups.UpdateCategoryGroup;
using GameAtlas.Application.Games.CreateGame;
using GameAtlas.Application.Games.ListGames;
using GameAtlas.Application.Games.UpdateGame;
using GameAtlas.Application.Markers;
using GameAtlas.Application.Markers.CreateMarker;
using GameAtlas.Application.Markers.Media;
using GameAtlas.Application.Markers.Media.Image;
using GameAtlas.Application.Markers.Media.Video;
using GameAtlas.Application.Markers.Media.YouTubeVideo;
using GameAtlas.Application.Markers.UpdateMarker;
using Microsoft.AspNetCore.Routing;

namespace GameAtlas.Api;

public static class DependencyInjection
{
	public static IServiceCollection AddApi(this IServiceCollection services)
	{
		services.AddProblemDetails();
		services.AddOpenApiValidationSource<Maps.CreateMapRequest, Application.Maps.CreateMap.CreateMap>();
		services.AddOpenApiValidationSource<Maps.MapUploadRequest, Application.Maps.Uploads.MapUploadMetadata>();
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
		services.AddOpenApiValidationSource<ImageMediaRequest, ImageMediaInput>();
		services.AddOpenApiValidationSource<YouTubeVideoMediaRequest, YouTubeVideoMediaInput>();
		services.AddOpenApiValidationSource<VideoMediaRequest, VideoMediaInput>();
		services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.AddOptionalSupport();
			options.SerializerOptions.TypeInfoResolver = options.SerializerOptions.TypeInfoResolver?
				.WithAddedModifier(MarkerMediaJsonTypeInfoModifier.Modify);

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
			options.AddOperationTransformer<Maps.MapOperationTransformer>();
			options.AddOptionalSupport();
			options.AddOperationTransformer<CamelCaseQueryParametersTransformer>();
			options.AddSchemaTransformer<FluentValidationSchemaTransformer>();
		});

		return services;
	}
}
