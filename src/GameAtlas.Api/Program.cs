using GameAtlas.Api;
using GameAtlas.Api.Categories;
using GameAtlas.Api.CategoryGroups;
using GameAtlas.Api.Games;
using GameAtlas.Api.Http;
using GameAtlas.Api.Maps;
using GameAtlas.Api.Maps.Events;
using GameAtlas.Api.Markers;
using GameAtlas.Api.Media;
using GameAtlas.Api.Results;
using GameAtlas.Application;
using GameAtlas.Infrastructure;
using GameAtlas.Infrastructure.Media.Inspection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddApplication()
	.AddInfrastructure(builder.Configuration)
	.AddApi()
	.AddMapsApi()
	.AddMediaApi();

var app = builder.Build();

app.Services.GetRequiredService<VideoInspector>().CheckCapabilities();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseMapTiles();
app.UseMediaFiles();

var api = app.MapGroup("").WithOutcomeResults().WithCaseInsensitiveEnumQueryParameters();

api.MapGames();
api.MapCategoryGroups();
api.MapCategories();
api.MapMarkers();
api.MapMaps();
api.MapMedia();
app.MapHub<MapHub>("/hubs/map");

app.Run();

public partial class Program;
