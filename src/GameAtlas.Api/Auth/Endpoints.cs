using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace GameAtlas.Api.Auth;

public static class Endpoints
{
	public static void MapAuth(this RouteGroupBuilder api)
	{
		var auth = api.MapGroup("/auth").WithTags("Auth").RequireAuthorization();

		auth
			.MapGet(
				"/antiforgery",
				(HttpContext http, IAntiforgery antiforgery) =>
				{
					var tokens = antiforgery.GetAndStoreTokens(http);

					http.Response.Cookies.Append(
						"XSRF-TOKEN",
						tokens.RequestToken!,
						new CookieOptions
						{
							Path = "/",
							Secure = true,
							SameSite = SameSiteMode.Strict
						});

					return TypedResults.NoContent();
				})
			.AllowAnonymous()
			.WithName("GetAntiforgery");

		auth
			.MapPost("/login", LoginAsync)
			.AllowAnonymous()
			.RequireRateLimiting("admin-login")
			.WithName("Login")
			.Produces(204)
			.ProducesProblem(StatusCodes.Status401Unauthorized)
			.ProducesProblem(StatusCodes.Status429TooManyRequests);

		auth
			.MapGet(
				"/session",
				(HttpContext http) =>
				{
					http.Response.Headers.CacheControl = "no-store";

					return new AdminSession(http.User.Identity?.Name ?? string.Empty);
				})
			.WithName("GetSession");

		auth
			.MapPost(
				"/logout",
				async (SignInManager<IdentityUser> manager) =>
				{
					await manager.SignOutAsync();

					return TypedResults.NoContent();
				})
			.WithName("Logout");
	}

	private static async Task<IResult> LoginAsync(AdminLogin input, SignInManager<IdentityUser> manager)
	{
		if (string.IsNullOrWhiteSpace(input.Login) || string.IsNullOrEmpty(input.Password))
			return InvalidCredentials();

		var result = await manager.PasswordSignInAsync(
			input.Login,
			input.Password,
			isPersistent: false,
			lockoutOnFailure: true);

		return result.Succeeded ? TypedResults.NoContent() : InvalidCredentials();
	}

	private static IResult InvalidCredentials() =>
		TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Incorrect username or password.");
}
