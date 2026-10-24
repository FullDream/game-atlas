using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Media;
using NetVips;

namespace GameAtlas.IntegrationTests;

public sealed class MediaUploadTests
{
	[Theory]
	[InlineData("png")]
	[InlineData("jpeg")]
	[InlineData("webp")]
	[InlineData("avif")]
	public async Task RasterUploadPersistsSharedMetadataAndUnchangedOriginal(string format)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var image = Image.Black(64, 48, bands: 4);
		var bytes = format switch
		{
			"png" => image.PngsaveBuffer(),
			"jpeg" => image.JpegsaveBuffer(),
			"webp" => image.WebpsaveBuffer(),
			"avif" => image.HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Av1),
			_ => throw new ArgumentOutOfRangeException(nameof(format))
		};
		using var body = new MultipartFormDataContent();

		body.Add(new ByteArrayContent(bytes), "file", "../../original.wrong");
		body.Add(new StringContent("Shared plain text"), "alt");
		using var response = await client.PostAsync("/media/assets", body);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var asset = Assert.IsType<ImageAssetDto>(await response.Content.ReadFromJsonAsync<MediaAssetDto>());

		Assert.Equal("original.wrong", asset.Name);
		Assert.Equal($"image/{format}", asset.ContentType);
		Assert.Equal("Shared plain text", asset.Alt);
		Assert.NotNull(asset.Placeholder);
		using var original = await client.GetAsync(asset.Url);

		Assert.Equal(asset.ContentType, original.Content.Headers.ContentType!.MediaType);
		Assert.Equal(bytes, await original.Content.ReadAsByteArrayAsync());
		Assert.Empty(Directory.EnumerateFiles(Path.Combine(factory.MediaStoragePath, "staging")));
		Assert.Single(Directory.EnumerateFiles(Path.Combine(factory.MediaStoragePath, "assets")));
	}

	[Theory]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg'><rect width='5' height='5'/></svg>", 200)]
	[InlineData("<svg xmlns='http://www.w3.org/2000/svg' onload='alert(1)'/>", 422)]
	[InlineData("not an image", 422)]
	public async Task SvgAndMalformedContentAreInspectedRatherThanTrustingFileNames(string text, int expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var body = new MultipartFormDataContent();

		body.Add(new StringContent(text), "file", "icon.svg");
		using var response = await client.PostAsync("/media/assets", body);

		Assert.Equal(expected, (int)response.StatusCode);

		if (expected == 200)
		{
			var asset = Assert.IsType<ImageAssetDto>(await response.Content.ReadFromJsonAsync<MediaAssetDto>());

			Assert.Equal("image/svg+xml", asset.ContentType);
			Assert.Null(asset.Placeholder);
		}
		else
		{
			Assert.Empty(Directory.EnumerateFiles(Path.Combine(factory.MediaStoragePath, "staging")));
		}
	}
}
