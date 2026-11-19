namespace GameAtlas.Api.Maps.Events;

internal static class MapHubOriginExtensions
{
	public static IApplicationBuilder UseMapHubOriginValidation(this WebApplication app)
	{
		var isDevelopment = app.Environment.IsDevelopment();

		return app.UseWhen(context => context.Request.Path.StartsWithSegments("/hubs/map"), branch =>
			branch.Use(async (context, next) =>
			{
				var request = context.Request;

				if (request.Headers.TryGetValue("Origin", out var origin) &&
					!string.Equals(origin, $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase) &&
					!(isDevelopment && origin.ToString() is "http://localhost:4201" or "https://localhost:4201"))
				{
					context.Response.StatusCode = StatusCodes.Status403Forbidden;

					return;
				}

				await next();
			}));
	}
}
