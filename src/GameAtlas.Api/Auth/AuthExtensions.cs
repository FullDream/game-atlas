using GameAtlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace GameAtlas.Api.Auth;

public static class AuthExtensions
{
	public static IServiceCollection AddAdminAuth(this IServiceCollection services, IWebHostEnvironment environment)
	{
		services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();

		services.ConfigureApplicationCookie(options =>
		{
			options.Cookie.Name = "__Host-GameAtlas.Session";
			options.Cookie.Path = "/";
			options.Cookie.HttpOnly = true;
			options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			options.Cookie.SameSite = SameSiteMode.Strict;
			options.ExpireTimeSpan = TimeSpan.FromHours(8);
			options.SlidingExpiration = false;
			options.Events.OnRedirectToLogin = context =>
			{
				context.Response.StatusCode = 401;

				return Task.CompletedTask;
			};
			options.Events.OnRedirectToAccessDenied = context =>
			{
				context.Response.StatusCode = 403;

				return Task.CompletedTask;
			};
		});

		services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager();

		services.AddAuthorization();

		services.AddAntiforgery(options =>
		{
			options.HeaderName = "X-XSRF-TOKEN";
			options.Cookie.Name = "__Host-GameAtlas.Antiforgery";
			options.Cookie.Path = "/";
			// Localhost dev-server forwarding uses HTTP. Cookie policy still emits Secure.
			options.Cookie.SecurePolicy = environment.IsDevelopment()
				? CookieSecurePolicy.SameAsRequest
				: CookieSecurePolicy.Always;
		});

		services.Configure<CookiePolicyOptions>(options => options.Secure = CookieSecurePolicy.Always);

		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			options.OnRejected = async (context, _) =>
			{
				if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == "admin-login")
					await TypedResults.Problem(
						statusCode: StatusCodes.Status429TooManyRequests,
						detail: "Too many attempts. Try again later.").ExecuteAsync(context.HttpContext);
			};

			options.AddFixedWindowLimiter(
				"admin-login",
				limiter =>
				{
					limiter.PermitLimit = 30;
					limiter.Window = TimeSpan.FromMinutes(1);
				});
		});

		return services;
	}

	public static async Task ValidateUnsafeRequest(HttpContext http, RequestDelegate next)
	{
		var method = http.Request.Method;

		if (http.GetEndpoint()?.Metadata.GetMetadata<AdminApiRequest>() is not null && !HttpMethods.IsGet(method) &&
			!HttpMethods.IsHead(method) && !HttpMethods.IsOptions(method))
		{
			var antiforgery = http.RequestServices.GetRequiredService<IAntiforgery>();

			if (!http.Request.Headers.ContainsKey("X-XSRF-TOKEN") || !await antiforgery.IsRequestValidAsync(http))
			{
				await TypedResults.Problem(statusCode: 400, title: "Invalid antiforgery token.").ExecuteAsync(http);

				return;
			}
		}

		await next(http);
	}
}

internal sealed class AdminApiRequest;
