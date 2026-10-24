using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Media.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameAtlas.IntegrationTests;

public class AddMapStyleApiTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LibraryAdmissionThenJsonAttachmentPreservesOriginalBytesAndMetadata(bool fileFirst)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var initial = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, initial);
		var map = await MapTestData.ReadAsync(created);
		var bytes = MapTestData.ImageBytes(color: 200);
		using var upload = MapTestData.Style(bytes, " Roads ", "#abc", fileFirst);
		using var response = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), upload);
		var result = await MapTestData.ReadAsync(response);
		var style = result.Styles.Last();
		var storage = factory.Services.GetRequiredService<IMediaStorage>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(map.Id, result.Id);
		Assert.Equal(2, result.Styles.Count);
		Assert.Equal("Roads", style.Name);
		Assert.Equal("#AABBCC", style.BackgroundColor);
		Assert.Null(style.TilesUrlTemplate);
		Assert.Equal(map.Styles.Single(), result.Styles.First());
		Assert.Equal(bytes, await File.ReadAllBytesAsync(storage.AssetPath(style.Source.Id)));
	}

	[Theory]
	[InlineData("missingFile")]
	[InlineData("duplicateFile")]
	[InlineData("duplicateName")]
	[InlineData("unknown")]
	[InlineData("batch")]
	public async Task MultipartInputReturns415WithoutAddingAStyle(string mode)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var map = await MapTestData.SeedAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var upload = new MultipartFormDataContent
		{
			{ new StringContent("Roads"), "name" },
			{ new StringContent("#fff"), "backgroundColor" }
		};

		if (mode != "missingFile")
			upload.Add(new ByteArrayContent(MapTestData.ImageBytes(1025, 513)), "file", "image.png");

		switch (mode)
		{
			case "duplicateFile":
				upload.Add(new ByteArrayContent(MapTestData.ImageBytes()), "file", "second.png");
				break;
			case "duplicateName":
				upload.Add(new StringContent("Other"), "NAME");
				break;
			case "unknown":
				upload.Add(new StringContent("ignored?"), "description");
				break;
			case "batch":
				upload.Add(new StringContent("[]"), "metadata");
				break;
		}

		using var response = await client.PostAsync($"{MapTestData.PathFor(game.Id)}/styles", upload);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
		Assert.Equal(map.Styles.Single().Id, (await MapTestData.ReadAsync(read)).Styles.Single().Id);
	}

	[Theory]
	[InlineData(" ", "#fff", "name")]
	[InlineData("Roads", "red", "backgroundColor")]
	public async Task MetadataFailuresUseFlatFieldNamesAndDoNotMutateMap(string name, string color, string field)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.SeedAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style(MapTestData.ImageBytes(1025, 513), name, color);
		using var response = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), upload);
		var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

		Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
		Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Single((await MapTestData.ReadAsync(read)).Styles);
	}

	[Fact]
	public async Task ActualFileBytesOverLimitReturn413WithoutAddingAStyle()
	{
		await using var factory = new GameApiFactory(configureServices: services =>
			services.AddSingleton<IOptions<MediaLimits>>(Options.Create(new MediaLimits { MaximumBytes = 10 })));

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.SeedAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style(MapTestData.ImageBytes(1025, 513));
		using var response = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), upload);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
		Assert.Single((await MapTestData.ReadAsync(read)).Styles);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InvalidImageOrMismatchedDimensionsReturn422WithoutAddingAStyle(bool invalidImage)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.SeedAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		var bytes = invalidImage ? Encoding.UTF8.GetBytes("<svg></svg>") : MapTestData.ImageBytes(23, 17);
		using var upload = MapTestData.Style(bytes);
		using var response = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), upload);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
		Assert.Single((await MapTestData.ReadAsync(read)).Styles);
	}

	[Fact]
	public async Task NonJsonRequestReturns415()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsync($"{MapTestData.PathFor(Guid.NewGuid())}/styles",
			new StringContent("not a form", Encoding.UTF8, "text/plain"));

		Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
	}
}
