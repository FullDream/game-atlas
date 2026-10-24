using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Infrastructure.Maps.Persistence;
using GameAtlas.Infrastructure.Maps.Processing;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapLifecycleTests
{
	[Fact]
	public async Task MapDeletionRemovesOwnedContentAndRecreationStartsEmptyWithoutChangingGame()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: JsonDocument.Parse(MarkerTestData.MixedMedia).RootElement.Clone());
		using var categoriesBefore = await client.GetAsync(CategoryTestData.Path(game.Id, group.Id));
		var before = await categoriesBefore.Content.ReadAsStringAsync();
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id));
		using var recreatedUpload = MapTestData.Style(MapTestData.ImageBytes(17, 9));
		using var recreated = await MapTestData.UploadFirstStyleAsync(client, game.Id, recreatedUpload);
		var next = await MapTestData.ReadAsync(recreated);
		using var markersRead = await client.GetAsync(MarkerTestData.Path(game.Id));
		var markers = await markersRead.Content.ReadFromJsonAsync<JsonElement>();
		using var categoriesAfter = await client.GetAsync(CategoryTestData.Path(game.Id, group.Id));

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.NotEqual(map.Id, next.Id);
		Assert.Equal(17, next.Width);
		Assert.Equal(0, markers.GetArrayLength());
		Assert.Equal(HttpStatusCode.NotFound, categoriesAfter.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		DeleteGameHandlerTests.AssertSameGame(game, await context.Games.SingleAsync());
		Assert.False(await context.CategoryGroups.AnyAsync());
		Assert.False(await context.Categories.AnyAsync());
	}

	[Fact]
	public async Task ConcurrentStyleDeletionCanRemoveAllStylesAndGameCascadeRetainsCleanupIntent()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();

		await MarkerTestData.CreateAsync(client, game.Id, category.Id);
		using var upload = MapTestData.Style();
		using var created = await MapTestData.UploadFirstStyleAsync(client, game.Id, upload);
		var map = await MapTestData.ReadAsync(created);
		using var secondUpload = MapTestData.Style(name: "Roads");
		using var added = await MapTestData.AddStyleAsync(client, MapTestData.PathFor(game.Id), secondUpload);
		var styles = (await MapTestData.ReadAsync(added)).Styles.ToArray();
		var deletions = await Task.WhenAll(styles.Select(style =>
			client.DeleteAsync($"{MapTestData.PathFor(game.Id)}/styles/{style.Id}")));

		try
		{
			Assert.Equal(new[] { HttpStatusCode.NoContent, HttpStatusCode.NoContent },
				deletions.Select(response => response.StatusCode).Order());
		}
		finally
		{
			foreach (var response in deletions)
				response.Dispose();
		}

		using var gameDeleted = await client.DeleteAsync($"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.NoContent, gameDeleted.StatusCode);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			Assert.False(await context.Games.AnyAsync());
			Assert.False(await context.Maps.AnyAsync());
			Assert.False(await context.MapStyles.AnyAsync());
			Assert.False(await context.Markers.AnyAsync());
			Assert.False(await context.Categories.AnyAsync());
			Assert.True(await context.Set<MapCleanupIntent>().AnyAsync(intent => intent.MapId == map.Id));
		}

		await factory.Services.GetRequiredService<MapProcessor>().CleanupAsync(CancellationToken.None);
		Assert.False(Directory.Exists(Path.Combine(factory.MapStoragePath, "content", map.Id.ToString("N"))));
	}
}
