using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using GameAtlas.Api.Categories;
using GameAtlas.Api.CategoryGroups;
using GameAtlas.Api.Errors;
using GameAtlas.Api.Games;
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
using Microsoft.AspNetCore.Routing;

namespace GameAtlas.Api;

public static class DependencyInjection
{
	public static IServiceCollection AddApi(this IServiceCollection services)
	{
		services.AddProblemDetails();
		services.AddExceptionHandler<GameSlugConflictExceptionHandler>();
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
			options.AddOptionalSupport();
			options.AddOperationTransformer<CamelCaseQueryParametersTransformer>();
			options.AddSchemaTransformer<FluentValidationSchemaTransformer>();
		});

		return services;
	}
}
