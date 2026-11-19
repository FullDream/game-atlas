using System.Net;
using System.Net.Http.Json;
using GameAtlas.Api.Auth;
using GameAtlas.Infrastructure.Identity;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameAtlas.IntegrationTests;

public class AdminAuthTests
{
	[Fact]
	public async Task ProvisioningRequiresValidSecretsOnlyForAnEmptyStore()
	{
		await using var factory = new GameApiFactory();
		await using var scope = factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var original = await db.Users.SingleAsync();
		var config = new ConfigurationManager();

		await AdministratorBootstrap.InitializeAsync(factory.Services, config);
		config["Admin:Login"] = "replacement";
		config["Admin:Password"] = "NewPassword7!";
		await AdministratorBootstrap.InitializeAsync(factory.Services, config);
		Assert.Equal(original.PasswordHash, (await db.Users.SingleAsync()).PasswordHash);
		await db.Users.ExecuteDeleteAsync();
		config["Admin:Password"] = null;
		await Assert.ThrowsAsync<InvalidOperationException>(() => AdministratorBootstrap.InitializeAsync(factory.Services, config));
		config["Admin:Password"] = "short";
		await Assert.ThrowsAsync<InvalidOperationException>(() => AdministratorBootstrap.InitializeAsync(factory.Services, config));
		Assert.Empty(await db.Users.ToListAsync());
	}

	[Fact]
	public async Task LoginRequiresCsrfAndKeepsFailureGeneric()
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateGuestClient();
		var credentials = new { login = "test-admin", password = "Test-only-password7!" };

		Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/auth/login", credentials)).StatusCode);
		await GameApiFactory.RefreshTokenAsync(client);
		var missing = await client.PostAsJsonAsync("/auth/login", new { login = "absent", password = "wrong" });

		Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);

		for (var i = 0; i < 5; i++)
			Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/auth/login", new { login = credentials.login, password = "wrong" })).StatusCode);

		var locked = await client.PostAsJsonAsync("/auth/login", credentials);

		Assert.Equal(missing.StatusCode, locked.StatusCode);
		Assert.Equal("Incorrect username or password.", (await missing.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
		Assert.Equal("Incorrect username or password.", (await locked.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
	}

	[Fact]
	public async Task LoginRateLimitReturnsProblemDetail()
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateGuestClient();

		await GameApiFactory.RefreshTokenAsync(client);
		var credentials = new { login = "", password = "" };

		for (var i = 0; i < 30; i++)
		{
			using var rejected = await client.PostAsJsonAsync("/auth/login", credentials);

			Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
		}

		using var limited = await client.PostAsJsonAsync("/auth/login", credentials);

		Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
		Assert.Equal("Too many attempts. Try again later.", (await limited.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
	}

	[Fact]
	public async Task SessionSurvivesRestartAndLogoutRequiresCsrf()
	{
		await using var first = new GameApiFactory();
		using var signedIn = await first.CreateHttpsClientAsync();
		var options = first.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);

		Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
		Assert.False(options.SlidingExpiration);
		Assert.True(options.Cookie.HttpOnly);
		Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Strict, options.Cookie.SameSite);
		Assert.Equal(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
		await using var second = new GameApiFactory(databasePath: first.DatabasePath);
		using var restored = second.CreateGuestClient();

		restored.DefaultRequestHeaders.Add("Cookie", first.SessionCookie);
		Assert.Equal("test-admin", (await restored.GetFromJsonAsync<AdminSession>("/auth/session"))!.Login);
		Assert.Equal(HttpStatusCode.BadRequest, (await restored.PostAsync("/auth/logout", null)).StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, (await restored.PostAsJsonAsync("/media/folders", new { name = "unsafe" })).StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, (await restored.PostAsync("/media/assets", new MultipartFormDataContent())).StatusCode);
		using var foreignHub = new HttpRequestMessage(HttpMethod.Post, "/hubs/map/negotiate?negotiateVersion=1");

		foreignHub.Headers.Add("Origin", "https://another.example");
		Assert.Equal(HttpStatusCode.Forbidden, (await signedIn.SendAsync(foreignHub)).StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, (await signedIn.PostAsync("/auth/logout", null)).StatusCode);
		Assert.Equal(HttpStatusCode.Unauthorized, (await signedIn.GetAsync("/auth/session")).StatusCode);
	}

	[Fact]
	public async Task ProductionRoutesDeclareAccessExplicitly()
	{
		await using var factory = new GameApiFactory();
		var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

		Assert.NotEmpty(endpoints);
		Assert.All(endpoints, endpoint =>
			Assert.True(endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null ||
				endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null, endpoint.DisplayName));
	}

	[Fact]
	public async Task OpenApiDistinguishesSessionAndPublicOperations()
	{
		await using var factory = new GameApiFactory(environment: "Development");
		using var guest = factory.CreateGuestClient();
		using var document = await guest.GetFromJsonAsync<System.Text.Json.JsonDocument>("/openapi/v1.json");
		var paths = document!.RootElement.GetProperty("paths");

		Assert.False(paths.GetProperty("/auth/login").GetProperty("post").TryGetProperty("security", out _));
		Assert.True(paths.GetProperty("/auth/session").GetProperty("get").GetProperty("security")[0].TryGetProperty("AdminCookie", out _));
		Assert.False(paths.GetProperty("/public/games").GetProperty("get").TryGetProperty("security", out _));
		Assert.False(paths.GetProperty("/auth/login").GetProperty("post").TryGetProperty("security", out _));
	}

	[Fact]
	public async Task GuestsOnlySeePublicResources()
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateGuestClient();

		Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/public/games")).StatusCode);
		Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/games")).StatusCode);
		Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/hubs/map/negotiate?negotiateVersion=1", null)).StatusCode);
		Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/media/assets", new MultipartFormDataContent())).StatusCode);
	}
}
