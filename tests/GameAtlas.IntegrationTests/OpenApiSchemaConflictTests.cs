using GameAtlas.Api;
using GameAtlas.Application.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace GameAtlas.IntegrationTests;

public sealed class OpenApiSchemaConflictTests
{
	[Fact]
	public async Task ConflictingConcreteMediaShapesFailGenerationInsteadOfDiscardingDifferences()
	{
		var builder = WebApplication.CreateBuilder();

		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
		{
			var schema = (OpenApiSchema)document.Components!.Schemas!["MediaItemImageAsset"];

			schema.Description = "Conflicting contextual representation";

			return Task.CompletedTask;
		}));
		builder.Services.AddApi();
		await using var app = builder.Build();

		app.Use(async (context, next) =>
		{
			try
			{
				await next(context);
			}
			catch (InvalidOperationException exception)
			{
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsync(exception.Message);
			}
		});
		app.MapOpenApi();
		app.MapGet("/item", () => (MediaItemDto)null!);
		app.MapGet("/image", () => (ImageAssetDto)null!);
		await app.StartAsync();
		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
		using var response = await client.GetAsync("/openapi/v1.json");

		Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
		Assert.Equal("Conflicting OpenAPI representations for ImageAsset.", await response.Content.ReadAsStringAsync());
	}
}
