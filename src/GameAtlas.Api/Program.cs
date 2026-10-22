using GameAtlas.Api;
using GameAtlas.Api.Http;
using GameAtlas.Api.Results;
using GameAtlas.Api.Games;
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

app.Run();

public partial class Program;
