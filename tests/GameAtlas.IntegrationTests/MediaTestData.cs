using GameAtlas.Domain.Media;
using GameAtlas.Application.Media;
using System.Net.Http.Json;
using System.Text;
using NetVips;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

internal static class MediaTestData
{
	internal static readonly Guid IconId = Guid.Parse("019a0000-0000-7000-8000-000000000111");
	internal static readonly Guid ImageId = Guid.Parse("019a0000-0000-7000-8000-000000000112");
	internal static readonly Guid VideoId = Guid.Parse("019a0000-0000-7000-8000-000000000113");
	internal static readonly Guid PreviewId = Guid.Parse("019a0000-0000-7000-8000-000000000114");

	internal static async Task EnsureMarkerAssetsAsync(GameApiFactory factory)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		if (await context.MediaAssets.AnyAsync(asset => asset.Id == ImageId))
			return;

		context.MediaAssets.AddRange(
			ImageAsset.Create(ImageId, "Image", null, "image/png", 64, 640, 480).Value!,
			ImageAsset.Create(PreviewId, "Preview", null, "image/jpeg", 64, 320, 180).Value!,
			VideoAsset.Create(VideoId, "Video", null, "video/mp4", 120, PreviewId).Value!);

		await context.SaveChangesAsync();
	}

	internal static async Task<ImageAssetDto> UploadImageAsync(HttpClient client, string format = "png",
		int width = 64, int height = 48)
	{
		using var image = Image.Black(width, height, bands: 4);
		var bytes = format switch
		{
			"png" => image.PngsaveBuffer(),
			"jpeg" => image.JpegsaveBuffer(),
			"webp" => image.WebpsaveBuffer(),
			"avif" => image.HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Av1),
			"svg" => Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'><rect width='8' height='8'/></svg>"),
			_ => throw new ArgumentOutOfRangeException(nameof(format))
		};
		using var body = new MultipartFormDataContent();

		body.Add(new ByteArrayContent(bytes), "file", $"image.{format}");
		using var response = await client.PostAsync("/media/assets", body);

		response.EnsureSuccessStatusCode();

		return Assert.IsType<ImageAssetDto>(await response.Content.ReadFromJsonAsync<MediaAssetDto>());
	}

	internal static async Task<ImageAsset> EnsureIconAsync(GameApiFactory factory)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var existing = await context.MediaAssets.OfType<ImageAsset>().SingleOrDefaultAsync(asset => asset.Id == IconId);

		if (existing is not null)
			return existing;

		var image = ImageAsset.Create(IconId, "Fixture icon", null, "image/svg+xml", 42, null, null).Value!;

		context.MediaAssets.Add(image);
		await context.SaveChangesAsync();

		return image;
	}
}
