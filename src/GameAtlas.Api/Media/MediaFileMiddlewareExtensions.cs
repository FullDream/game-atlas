using GameAtlas.Application.Media.Storage;
using Microsoft.Extensions.FileProviders;

namespace GameAtlas.Api.Media;

public static class MediaFileMiddlewareExtensions
{
	public static IApplicationBuilder UseMediaFiles(this WebApplication app)
	{
		var storage = app.Services.GetRequiredService<IMediaStorage>();
		var files = new PhysicalFileProvider(storage.PublicAssetsRoot);

		app.Lifetime.ApplicationStopped.Register(files.Dispose);

		return app.Map("/media/files", branch =>
		{
			branch.Use(async (context, next) =>
			{
				var value = context.Request.Path.Value;

				if (value is null || value.Length < 2 ||
					!Guid.TryParseExact(value.AsSpan(1), "D", out var id) || id == Guid.Empty)
				{
					context.Response.StatusCode = StatusCodes.Status404NotFound;

					return;
				}

				var originalPath = context.Request.Path;

				context.Request.Path = $"/{id:N}";

				try
				{
					await next(context);
				}
				finally
				{
					context.Request.Path = originalPath;
				}
			});

			branch.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = files,
				ContentTypeProvider = new MediaFileContentTypeProvider(storage),
				OnPrepareResponse = context =>
				{
					var headers = context.Context.Response.Headers;

					headers.XContentTypeOptions = "nosniff";
					headers.CacheControl = "public,max-age=31536000,immutable";

					if (context.Context.Response.ContentType == "image/svg+xml")
						headers.ContentSecurityPolicy =
							"sandbox; default-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
				}
			});
		});
	}
}
