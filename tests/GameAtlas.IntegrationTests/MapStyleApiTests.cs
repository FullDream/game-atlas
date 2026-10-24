using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Media.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapStyleApiTests
{
	[Fact]
	public async Task OriginalCannotBeReplacedAndProcessingFieldsCannotBePatched()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		var style = map.Styles.Single();
		var storage = factory.Services.GetRequiredService<IMediaStorage>();
		using var replacement = new MultipartFormDataContent
		{
			{ new ByteArrayContent(MapTestData.ImageBytes(color: 200)), "file", "replacement.png" }
		};
		using var rejected = await client.PostAsync(
			$"{MapTestData.PathFor(game.Id)}/styles/{style.Id}/image", replacement);

		Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);

		foreach (var field in new[] { "status", "progress", "sourceVersionId", "activeVersionId", "tilesUrlTemplate" })
		{
			using var patch = await client.PatchAsJsonAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}",
				new Dictionary<string, object?> { [field] = null, ["name"] = "Changed" });

			Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
		}

		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(style, (await MapTestData.ReadAsync(read)).Styles.Single());
		Assert.Equal(MapTestData.ImageBytes(), await File.ReadAllBytesAsync(storage.AssetPath(style.Source.Id)));
	}
}
