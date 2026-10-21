using GameAtlas.Api;
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

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();

var api = app.MapGroup("").WithOutcomeResults();

app.Run();
