using System.Net;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Maps.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapAdmissionTests
{
	[Fact]
	public async Task CreationOrderAndDeletingDefaultChooseNextReadyStyle()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var created = await MapTestData.CreateWithStylesAsync(client, game.Id);
		var map = await MapTestData.ReadAsync(created);
		var styles = map.Styles.ToArray();

		Assert.Equal(new[] { "Atlas", "Roads" }, styles.Select(style => style.Name));
		var storage = factory.Services.GetRequiredService<IMediaStorage>();

		Assert.Equal(MapTestData.ImageBytes(), await File.ReadAllBytesAsync(
			storage.AssetPath(styles[0].Source.Id)));
		Assert.Equal(MapTestData.ImageBytes(color: 100), await File.ReadAllBytesAsync(
			storage.AssetPath(styles[1].Source.Id)));
		var processor = factory.Services.GetRequiredService<MapProcessor>();

		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		Assert.True(await processor.RunNextAsync(CancellationToken.None));
		using var deleted = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{styles[0].Id}");
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var stored = await MapTestData.ReadAsync(read);

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Equal(styles[1].Id, stored.DefaultStyleId);
		Assert.Equal(MapProcessingStatus.Ready, stored.Styles.Single().Status);
		using var final = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{styles[1].Id}");

		Assert.Equal(HttpStatusCode.NoContent, final.StatusCode);
	}

	[Theory]
	[InlineData(" atlas ", false, 409)]
	[InlineData("Roads", true, 422)]
	public async Task DuplicateNamesAndDifferentDimensionsRejectAddition(string secondName,
		bool differentDimensions, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var initial = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, initial);
		var map = await MapTestData.ReadAsync(created);
		using var upload = MapTestData.Style(MapTestData.ImageBytes(differentDimensions ? 24 : 23), secondName);
		using var response = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), upload);

		Assert.Equal(status, (int)response.StatusCode);
		using var missing = await client.GetAsync(MapTestData.PathFor(game.Id));

		Assert.Equal(map.Styles.Single(), (await MapTestData.ReadAsync(missing)).Styles.Single());
	}

	[Fact]
	public async Task ExistingMapAndStyleConflictsAndMissingOwnersLeaveCurrentStateUnchanged()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var first = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, first);
		var map = await MapTestData.ReadAsync(created);
		using var duplicate = MapTestData.Style();
		using var duplicateMap = await MapTestData.UploadFirstStyleAsync(client, game.Id, duplicate);
		using var duplicateStyle = MapTestData.Style(name: " atlas ");
		using var duplicateAdd = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), duplicateStyle);
		using var missingUpload = MapTestData.Style();
		using var missing = await MapTestData.UploadFirstStyleAsync(client, Guid.NewGuid(), missingUpload);

		Assert.Equal(HttpStatusCode.Conflict, duplicateMap.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, duplicateAdd.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var stored = await MapTestData.ReadAsync(read);

		Assert.Equal(map.Id, stored.Id);
		Assert.Equal(map.UpdatedAt, stored.UpdatedAt);
		Assert.Equal(map.Styles.Single(), stored.Styles.Single());
	}

}
