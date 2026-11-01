using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Maps;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MultiMapApiTests
{
	[Fact]
	public async Task StyleOperationsRejectSiblingMapsAndWrongGamesWithoutChangingEitherMap()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "styles");
		var other = await UpdateGameHandlerTests.SeedAsync(factory, "other-styles");
		using var client = factory.CreateHttpsClient();
		using var firstCreation = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = "World" });
		using var secondCreation = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = "Dungeon" });
		var first = await MapTestData.ReadAsync(firstCreation);
		var second = await MapTestData.ReadAsync(secondCreation);
		using var upload = MapTestData.Style();
		using var admitted = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id, first.Id), upload);
		var style = (await MapTestData.ReadAsync(admitted)).Styles.Single();

		foreach (var scope in new[] { (game.Id, second.Id), (other.Id, first.Id) })
		{
			var path = MapTestData.PathFor(scope.Item1, scope.Item2) + $"/styles/{style.Id}";
			using var patch = await client.PatchAsJsonAsync(path, new { name = "Changed" });
			using var retry = await client.PostAsync(path + "/retry", null);
			using var deleted = await client.DeleteAsync(path);

			Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
			Assert.Equal(HttpStatusCode.NotFound, retry.StatusCode);
			Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
		}

		using var wrongUpload = MapTestData.Style();
		using var rejected = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(other.Id, first.Id), wrongUpload);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id, first.Id));
		using var siblingRead = await client.GetAsync(MapTestData.PathFor(game.Id, second.Id));

		Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
		Assert.Equal("Atlas", (await MapTestData.ReadAsync(read)).Styles.Single().Name);
		Assert.Empty((await MapTestData.ReadAsync(siblingRead)).Styles);
	}

	[Fact]
	public async Task MissingParentsUnsupportedMediaAndLegacyRoutesAreRejected()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "boundaries");
		using var client = factory.CreateHttpsClient();
		using var missingList = await client.GetAsync(MapTestData.CollectionFor(Guid.NewGuid()));
		using var missingCreate = await client.PostAsJsonAsync(MapTestData.CollectionFor(Guid.NewGuid()),
			new { name = "World" });
		using var unsupported = await client.PostAsync(MapTestData.CollectionFor(game.Id),
			new StringContent("{}", Encoding.UTF8, "text/plain"));

		Assert.Equal(HttpStatusCode.NotFound, missingList.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, missingCreate.StatusCode);
		Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);

		foreach (var path in new[] { "map", "category-groups", "markers" })
		{
			using var oldRoute = await client.GetAsync($"/games/{game.Id}/{path}");

			Assert.Equal(HttpStatusCode.NotFound, oldRoute.StatusCode);
		}
	}

	[Fact]
	public async Task JsonCreationAndListingKeepIndependentBlankCoordinateSpacesWithoutUploadWork()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var other = await UpdateGameHandlerTests.SeedAsync(factory, "other");
		using var client = factory.CreateHttpsClient();
		using var empty = await client.GetAsync(MapTestData.CollectionFor(game.Id));

		Assert.Equal("[]", await empty.Content.ReadAsStringAsync());
		using var first = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = " World " });
		var world = await MapTestData.ReadAsync(first);
		using var second = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = "Dungeon" });
		var dungeon = await MapTestData.ReadAsync(second);
		using var sameNameElsewhere = await client.PostAsJsonAsync(MapTestData.CollectionFor(other.Id),
			new { name = "World" });
		using var duplicate = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id),
			new { name = " world " });
		using var foreign = await client.GetAsync(MapTestData.PathFor(other.Id, world.Id));
		using var listing = await client.GetAsync(MapTestData.CollectionFor(game.Id));
		var maps = (await listing.Content.ReadFromJsonAsync<MapDto[]>(MapTestData.JsonOptions))!;

		Assert.Equal(HttpStatusCode.OK, sameNameElsewhere.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
		Assert.Equal(new[] { world.Id, dungeon.Id }, maps.Select(map => map.Id));
		Assert.Equal("World", world.Name);
		Assert.Null(world.Extent);
		Assert.Null(world.Width);
		Assert.Null(world.Height);
		Assert.All(maps, map => Assert.Empty(map.Styles));
		Assert.All(maps, map => Assert.Null(map.DefaultStyleId));
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.False(await context.MapStyles.AnyAsync());
		Assert.Empty(Directory.EnumerateFiles(factory.MapStoragePath, "*", SearchOption.AllDirectories));
		using var catalog = await client.GetAsync("/games");
		using var catalogJson = JsonDocument.Parse(await catalog.Content.ReadAsStringAsync());

		Assert.All(catalogJson.RootElement.GetProperty("items").EnumerateArray(), item =>
		{
			Assert.Equal(14, item.EnumerateObject().Count());
			Assert.False(item.TryGetProperty("mapId", out _));
		});
	}

	[Theory]
	[InlineData("{}", 422)]
	[InlineData("""{"name":"World","width":null,"height":1}""", 400)]
	[InlineData("""{"name":"World","width":0,"height":1}""", 400)]
	[InlineData("""{"name":"World","width":16385,"height":1}""", 400)]
	[InlineData("""{"name":"World","width":1.5,"height":1}""", 400)]
	[InlineData("[]", 400)]
	public async Task CreationValidatesJsonWithoutPersistingPartialMaps(string json, int expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsync(MapTestData.CollectionFor(game.Id),
			new StringContent(json, Encoding.UTF8, "application/json"));

		Assert.Equal(expected, (int)response.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();

		Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Maps.AnyAsync());
	}

	[Fact]
	public async Task FirstStyleInitializesGeometryAndLastStyleCanBeRemovedWithoutRemovingContent()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id);
		using var invalidFile = MapTestData.Style(MapTestData.ImageBytes(24, 17), name: "First");
		using var first = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), invalidFile);
		using var validFile = MapTestData.Style();
		using var admitted = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), validFile);
		var map = await MapTestData.ReadAsync(admitted);
		using var deleted = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{map.Styles.Last().Id}");
		using var removedFirst = await client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{map.Styles.First().Id}");
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id));
		var blank = await MapTestData.ReadAsync(read);

		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		Assert.Equal(24, map.Width);
		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Equal(map.Id, blank.Id);
		Assert.Equal(map.Extent, blank.Extent);
		Assert.Empty(blank.Styles);
		Assert.Null(blank.DefaultStyleId);
		using var markers = await client.GetAsync(MarkerTestData.Path(game.Id));
		var items = await markers.Content.ReadFromJsonAsync<JsonElement>();

		Assert.Equal(marker.GetRawText(), items[0].GetRawText());
	}
}
