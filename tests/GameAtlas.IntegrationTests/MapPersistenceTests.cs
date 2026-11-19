using GameAtlas.Domain.ValueObjects;
using GameAtlas.Domain.Maps;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapPersistenceTests
{
	[Fact]
	public async Task FreshSchemaPersistsMapsAndRollbackPreservesAllParentAndMarkerData()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "legacy-id");

		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = await factory.CreateHttpsClientAsync();

		var marker = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
			media: System.Text.Json.JsonDocument.Parse(MarkerTestData.MixedMedia).RootElement.Clone());

		var markerId = marker.GetProperty("id").GetGuid();
		var map = await MapTestData.SeedAsync(factory, game.Id);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			var stored = await context.Maps.Include(map => map.Styles)
				.SingleAsync();

			Assert.Equal(map.Id, stored.Id);
			Assert.Equal(map.Geometry, stored.Geometry);
			Assert.Equal(map.Styles.Single().Id, stored.Styles.Single().Id);
			Assert.Equal(MapProcessingStatus.Queued, stored.Styles.Single().Status);
			Assert.False(context.Database.HasPendingModelChanges());
			var duplicate = Map.Create(game.Id, map.Name, Slug.Create("map-" + Guid.NewGuid().ToString("N")).Value!).Value!;

			context.Maps.Add(duplicate);
			var conflict = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

			Assert.True(new PostgresPersistenceFailureClassifier().IsMapNameConflict(conflict));
		}

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			DeleteGameHandlerTests.AssertSameGame(game, await context.Games.SingleAsync());
			Assert.Equal(group.Id, (await context.CategoryGroups.SingleAsync()).Id);
			Assert.Equal(category.Id, (await context.Categories.SingleAsync()).Id);
			var storedMarker = await context.Markers.Include(marker => marker.Media).SingleAsync();

			Assert.Equal(markerId, storedMarker.Id);
			Assert.Equal(3, storedMarker.Media.Count);
			Assert.Equal(map.Id, (await context.Maps.SingleAsync()).Id);
		}
	}
}
