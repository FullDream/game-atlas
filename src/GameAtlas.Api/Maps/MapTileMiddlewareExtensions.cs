using GameAtlas.Application.Maps.Storage;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace GameAtlas.Api.Maps;

public static class MapTileMiddlewareExtensions
{
	public static IApplicationBuilder UseMapTiles(this WebApplication app)
	{
		var storage = app.Services.GetRequiredService<IMapStorage>();
		var files = new PhysicalFileProvider(storage.PublicTilesRoot);
		var contentTypes = new FileExtensionContentTypeProvider(new Dictionary<string, string>
		{
			[".webp"] = "image/webp"
		});

		app.Lifetime.ApplicationStopped.Register(files.Dispose);

		return app.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = files,
			RequestPath = "/map-tiles",
			ContentTypeProvider = contentTypes,
			OnPrepareResponse = context =>
				context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
		});
	}
}
