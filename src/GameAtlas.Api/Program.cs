using GameAtlas.Api;
using GameAtlas.Api.Categories;
using GameAtlas.Api.CategoryGroups;
using GameAtlas.Api.Games;
using GameAtlas.Api.Http;
using GameAtlas.Api.Markers;
using GameAtlas.Api.Results;
using GameAtlas.Application;
using GameAtlas.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddApplication()
	.AddInfrastructure(builder.Configuration)
	.AddApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();

var api = app.MapGroup("").WithOutcomeResults().WithCaseInsensitiveEnumQueryParameters();

api.MapGames();
api.MapCategoryGroups();
api.MapCategories();
api.MapMarkers();

app.Run();

public partial class Program;
