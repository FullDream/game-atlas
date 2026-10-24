using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameAtlas.Application.Maps;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Media;
using GameAtlas.Application.Media;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetVips;

namespace GameAtlas.IntegrationTests;

internal static class MapTestData
{
	internal static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private static readonly ConcurrentDictionary<Guid, Guid> mapIds = new();

	internal static Guid MapIdFor(Guid gameId) => mapIds.GetValueOrDefault(gameId, gameId);

	internal static string CollectionFor(Guid gameId) => $"/games/{gameId}/maps";

	internal static string PathFor(Guid gameId, Guid? mapId = null) => $"{CollectionFor(gameId)}/{mapId ?? MapIdFor(gameId)}";

	internal static async Task<Map> EnsureMapAsync(GameApiFactory factory, Guid gameId,
		int width = 23, int height = 17)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var map = await context.Maps.Where(map => map.GameId == gameId).OrderBy(map => map.CreatedAt)
			.FirstOrDefaultAsync();

		if (map is null)
		{
			map = Map.Create(gameId, "World", width, height).Value!;
			context.Maps.Add(map);
			await context.SaveChangesAsync();
		}

		mapIds[gameId] = map.Id;

		return map;
	}

	internal static async Task<HttpResponseMessage> UploadFirstStyleAsync(HttpClient client, Guid gameId,
		MultipartFormDataContent upload)
	{
		var listing = await client.GetAsync(CollectionFor(gameId));

		if (!listing.IsSuccessStatusCode)
			return listing;

		using (listing)
		{
			var maps = (await listing.Content.ReadFromJsonAsync<MapDto[]>(JsonOptions))!;
			var map = maps.FirstOrDefault(item => item.Id == MapIdFor(gameId)) ?? maps.FirstOrDefault();

			if (map is null)
			{
				var width = 23;
				var height = 17;
				var file = upload.FirstOrDefault(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "file");

				if (file is not null)
				{
					try
					{
						using var image = Image.NewFromBuffer(await file.ReadAsByteArrayAsync());

						width = image.Width;
						height = image.Height;
					}
					catch (VipsException)
					{
						// Invalid originals still reach the style admission boundary.
					}
				}

				var created = await client.PostAsJsonAsync(CollectionFor(gameId), new { name = "World", width, height });

				if (!created.IsSuccessStatusCode)
					return created;

				using (created)
					map = await ReadAsync(created);
			}

			mapIds[gameId] = map.Id;

			return await AddStyleAsync(client, PathFor(gameId), upload);
		}
	}

	internal static string TilesPath(Guid mapId, Guid styleId) => $"/map-tiles/{mapId:N}/{styleId:N}";

	internal static byte[] ImageBytes(int width = 23, int height = 17, double color = 0)
	{
		using var black = Image.Black(width, height, bands: 3);
		using var image = black.NewFromImage([color, color, color]);

		return image.PngsaveBuffer();
	}

	internal static MultipartFormDataContent Style(byte[]? image = null, string name = "Atlas",
		string color = "#fff", bool fileFirst = false)
	{
		var content = new MultipartFormDataContent();
		var file = new ByteArrayContent(image ?? ImageBytes());

		if (fileFirst)
			content.Add(file, "file", "misleading.svg");

		content.Add(new StringContent(name), "name");
		content.Add(new StringContent(color), "backgroundColor");

		if (!fileFirst)
			content.Add(file, "file", "misleading.svg");

		return content;
	}

	internal static async Task<HttpResponseMessage> CreateWithStylesAsync(HttpClient client, Guid gameId)
	{
		using var initial = Style();
		using var created = await UploadFirstStyleAsync(client, gameId, initial);

		created.EnsureSuccessStatusCode();
		using var next = Style(ImageBytes(color: 100), "Roads", "#000");

		return await AddStyleAsync(client, PathFor(gameId), next);
	}

	internal static async Task<MapDto> ReadAsync(HttpResponseMessage response)
	{
		Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

		return (await response.Content.ReadFromJsonAsync<MapDto>(JsonOptions))!;
	}

	private static JsonSerializerOptions CreateOptions()
	{
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

		options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

		return options;
	}

	internal static async Task<Map> SeedAsync(GameApiFactory factory, Guid gameId)
	{
		var seeded = await EnsureMapAsync(factory, gameId, 1025, 513);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var map = await context.Maps.AsTracking().WithStyles().SingleAsync(item => item.Id == seeded.Id);

		if (map.Styles.Count == 0)
		{
			var bytes = ImageBytes(map.Width, map.Height);
			var image = ImageAsset.Create(Guid.CreateVersion7(), "Source", null, "image/png", bytes.Length,
				map.Width, map.Height).Value!;
			var storage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();
			var path = storage.AssetPath(image.Id);

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			await File.WriteAllBytesAsync(path, bytes);
			Assert.True(map.AddStyle(new MapStyleInput("Atlas", "#fff", image)).IsSuccess);
			await context.SaveChangesAsync();
		}

		return map;
	}

	internal static async Task<HttpResponseMessage> AddStyleAsync(HttpClient client, string mapPath,
		MultipartFormDataContent input)
	{
		var file = input.FirstOrDefault(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "file");
		using var upload = new MultipartFormDataContent();

		if (file is not null)
			upload.Add(new ByteArrayContent(await file.ReadAsByteArrayAsync()), "file",
				file.Headers.ContentDisposition?.FileName?.Trim('"') ?? "source");

		var admitted = await client.PostAsync("/media/assets", upload);

		if (!admitted.IsSuccessStatusCode)
			return admitted;

		using (admitted)
		{
			var asset = (await admitted.Content.ReadFromJsonAsync<MediaAssetDto>())!;
			var name = input.FirstOrDefault(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "name");
			var color = input.FirstOrDefault(part => part.Headers.ContentDisposition?.Name?.Trim('"') == "backgroundColor");

			return await client.PostAsJsonAsync(mapPath + "/styles", new
			{
				name = name is null ? null : await name.ReadAsStringAsync(),
				backgroundColor = color is null ? null : await color.ReadAsStringAsync(),
				sourceAssetId = asset.Id
			});
		}
	}

}
