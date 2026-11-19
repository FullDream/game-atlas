using GameAtlas.Api.Auth;
using GameAtlas.Api;
using GameAtlas.Api.Categories;
using GameAtlas.Api.Games;
using GameAtlas.Api.Http;
using GameAtlas.Api.Maps;
using GameAtlas.Api.Maps.Events;
using GameAtlas.Api.Markers;
using GameAtlas.Api.Media;
using GameAtlas.Api.Results;
using GameAtlas.Application;
using GameAtlas.Infrastructure;
using GameAtlas.Infrastructure.Identity;
using GameAtlas.Infrastructure.Media.Inspection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddApplication()
	.AddInfrastructure(builder.Configuration)
	.AddApi()
	.AddAdminAuth(builder.Environment)
	.AddMapsApi()
	.AddMediaApi();

var app = builder.Build();

app.Services.GetRequiredService<VideoInspector>().CheckCapabilities();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi().AllowAnonymous();
	app.MapScalarApiReference().AllowAnonymous();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto });
app.UseHttpsRedirection();
app.UseMapTiles();
app.UseMediaFiles();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(AuthExtensions.ValidateUnsafeRequest);
app.UseMapHubOriginValidation();

var api = app.MapGroup("").WithOutcomeResults().WithCaseInsensitiveEnumQueryParameters();

api.WithMetadata(new AdminApiRequest());
api.MapAuth();
api.MapGames();
api.MapPublicGames();
api.MapCategories();
api.MapMarkers();
api.MapMaps();
api.MapPublicMaps();
api.MapMedia();
app.MapHub<MapHub>("/hubs/map", options => options.CloseOnAuthenticationExpiration = true).RequireAuthorization();

await AdministratorBootstrap.InitializeAsync(app.Services, app.Configuration);
app.Run();

public partial class Program;
