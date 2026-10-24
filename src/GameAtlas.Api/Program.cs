using GameAtlas.Api;
using GameAtlas.Api.Categories;
using GameAtlas.Api.CategoryGroups;
using GameAtlas.Api.Games;
using GameAtlas.Api.Http;
using GameAtlas.Api.Maps;
using GameAtlas.Api.Markers;
using GameAtlas.Api.Results;
using GameAtlas.Application;
using GameAtlas.Infrastructure;
using GameAtlas.Infrastructure.Maps.Persistence;
using Scalar.AspNetCore;

if (args.FirstOrDefault() == "prepare-map-ownership")
{
	if (args.Length != 3)
	{
		Console.Error.WriteLine("Usage: prepare-map-ownership <database-path> <mapping-json-path>");
		Environment.ExitCode = 1;

		return;
	}

	var preparation = await MapOwnershipPreparation.PrepareFileAsync(args[1], args[2]);

	if (preparation.IsFailure)
	{
		foreach (var error in preparation.Errors)
			Console.Error.WriteLine(error.Message);

		Environment.ExitCode = 1;
	}

	return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddApplication()
	.AddInfrastructure(builder.Configuration)
	.AddApi()
	.AddMapsApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseMapTiles();

var api = app.MapGroup("").WithOutcomeResults().WithCaseInsensitiveEnumQueryParameters();

api.MapGames();
api.MapCategoryGroups();
api.MapCategories();
api.MapMarkers();
api.MapMaps();

app.Run();

public partial class Program;
