using System.Net;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameAtlas.IntegrationTests;

public class MapMultipartTests
{
	[Theory]
	[InlineData("name")]
	[InlineData("backgroundColor")]
	public async Task MissingMetadataReturns422WithFlatFieldName(string missingField)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var content = new MultipartFormDataContent
		{
			{ new ByteArrayContent(MapTestData.ImageBytes()), "file", "image.png" }
		};

		if (missingField != "name")
			content.Add(new StringContent("Atlas"), "name");

		if (missingField != "backgroundColor")
			content.Add(new StringContent("#fff"), "backgroundColor");

		using var response = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), content);
		using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
		Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(missingField, out _));
		Assert.Empty(Directory.EnumerateFiles(factory.MapStoragePath, "*", SearchOption.AllDirectories));
	}

	[Theory]
	[InlineData("", "#fff", "name")]
	[InlineData("Atlas", "red", "backgroundColor")]
	public async Task InvalidMetadataReturns422WithFlatFieldNames(string name, string color, string field)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var content = MapTestData.Style(name: name, color: color);
		using var response = await MapTestData.UploadFirstStyleAsync(client, game.Id, content);
		using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
		Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(field, out _));
		Assert.Empty(Directory.EnumerateFiles(factory.MapStoragePath, "*", SearchOption.AllDirectories));
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.False(await context.MapStyles.AnyAsync());
	}

	[Theory]
	[InlineData("application/octet-stream")]
	[InlineData("text/plain")]
	public async Task UnsupportedRequestMediaReturns415(string contentType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsync($"{MapTestData.PathFor(Guid.NewGuid())}/styles",
			new StringContent("{}", Encoding.UTF8, contentType));

		Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
	}

	[Theory]
	[InlineData("duplicateFile")]
	[InlineData("duplicateName")]
	[InlineData("missingFile")]
	[InlineData("unknown")]
	[InlineData("batch")]
	public async Task AmbiguousPartsReturn400(string mode)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var content = new MultipartFormDataContent
		{
			{ new StringContent("Atlas"), "name" },
			{ new StringContent(""), "alt" }
		};

		if (mode != "missingFile")
			content.Add(new ByteArrayContent(MapTestData.ImageBytes()), "file", "image.png");

		if (mode == "duplicateFile")
			content.Add(new ByteArrayContent(MapTestData.ImageBytes()), "file", "other.png");
		else if (mode == "duplicateName")
			content.Add(new StringContent("Other"), "NAME");
		else if (mode is "unknown" or "batch")
			content.Add(new StringContent("[]"), mode == "batch" ? "metadata" : "description");

		using var response = await client.PostAsync("/media/assets", content);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Empty(Directory.EnumerateFiles(factory.MapStoragePath, "*", SearchOption.AllDirectories));
	}

	[Theory]
	[InlineData("unsupported")]
	[InlineData("short")]
	[InlineData("dimension")]
	public async Task UnsupportedOrOversizeImageContentReturns422(string mode)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		var image = mode switch
		{
			"unsupported" => Encoding.UTF8.GetBytes("<svg>this is not a supported raster image</svg>"),
			"short" => new byte[3],
			_ => MapTestData.ImageBytes(16385, 1)
		};
		using var content = MapTestData.Style(image);
		using var response = await MapTestData.UploadFirstStyleAsync(client, game.Id, content);

		Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExactFileByteBoundaryWorksWithoutContentLengthAndOneByteOverFails(bool exceed)
	{
		var image = MapTestData.ImageBytes();
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IOptions<MediaLimits>>(Options.Create(new MediaLimits
			{
				MaximumBytes = image.Length
			})));

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var multipart = new MultipartFormDataContent();

		multipart.Add(new ByteArrayContent(exceed ? [.. image, 0] : image), "file", "original.png");
		using var request = new HttpRequestMessage(HttpMethod.Post, "/media/assets")
		{
			Content = new StreamContent(await multipart.ReadAsStreamAsync())
		};

		request.Content.Headers.ContentType = multipart.Headers.ContentType;
		request.Headers.TransferEncodingChunked = true;
		using var response = await client.SendAsync(request);

		Assert.Equal(exceed ? HttpStatusCode.RequestEntityTooLarge : HttpStatusCode.OK, response.StatusCode);
	}
}
