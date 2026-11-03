using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Domain.Markers;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MarkerLifecycleTests
{
	[Fact]
	public async Task ListIsCompleteScopedOrderedAndHasNoFilters()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		var (foreignGame, foreignGroup) = await CategoryTestData.ParentAsync(factory);

		var foreignCategory = await CategoryTestData.SeedAsync(factory, foreignGroup.Id);
		using var client = factory.CreateHttpsClient();
		using var empty = await client.GetAsync(MarkerTestData.Path(game.Id));

		Assert.Empty((await MarkerTestData.ReadAsync(empty)).EnumerateArray());

		var first = await MarkerTestData.CreateAsync(client, game.Id, category.Id, "Duplicate",
			JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));

		var second = await MarkerTestData.CreateAsync(client, game.Id, category.Id, "Duplicate");

		var foreign = await MarkerTestData.CreateAsync(client, foreignGame.Id, foreignCategory.Id);

		var ids = new[] { first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid() };

		var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			foreach (var marker in await db.Markers.AsTracking().Where(item => ids.Contains(item.Id)).ToArrayAsync())
				db.Entry(marker).Property(item => item.CreatedAt).CurrentValue = timestamp;

			await db.SaveChangesAsync();
		}

		using var listed = await client.GetAsync(
			MarkerTestData.Path(game.Id) + "?offset=999&limit=0&categoryId=" + foreignCategory.Id);

		var items = await MarkerTestData.ReadAsync(listed);

		Assert.Equal(ids.Order().ToArray(), items.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
		Assert.Equal(3, items[0].GetProperty("media").GetArrayLength() + items[1].GetProperty("media").GetArrayLength());
		Assert.All(items.EnumerateArray(), item => Assert.Equal("Description", item.GetProperty("description").GetString()));
		using var missing = await client.GetAsync(MarkerTestData.Path(Guid.NewGuid()));

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

		var foreignId = foreign.GetProperty("id").GetGuid();

		foreach (var markerId in new[] { Guid.NewGuid(), foreignId })
		{
			using var patch = await client.PatchAsJsonAsync(MarkerTestData.Path(game.Id) + $"/{markerId}", new { name = "No" });
			using var delete = await client.DeleteAsync(MarkerTestData.Path(game.Id) + $"/{markerId}");

			Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
			Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
		}

		using var invalidGuid = await client.GetAsync("/games/not-a-guid/maps/019a0000-0000-7000-8000-000000000001/markers");

		Assert.Equal(HttpStatusCode.BadRequest, invalidGuid.StatusCode);
	}

	[Fact]
	public async Task CreationDefaultsAndAllZeroMissingOrForeignCategoryChecks()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		var (_, foreignGroup) = await CategoryTestData.ParentAsync(factory);

		var foreignCategory = await CategoryTestData.SeedAsync(factory, foreignGroup.Id);
		using var client = factory.CreateHttpsClient();
		using var defaults = await client.PostAsJsonAsync(MarkerTestData.Path(game.Id),
			new { categoryId = category.Id, name = "Marker", position = new { x = 0, y = -0.5 } });

		var dto = await MarkerTestData.ReadAsync(defaults);

		Assert.Equal(JsonValueKind.Null, dto.GetProperty("description").ValueKind);
		Assert.Empty(dto.GetProperty("media").EnumerateArray());

		foreach (var destination in new[] { Guid.Empty, Guid.NewGuid(), foreignCategory.Id })
		{
			using var rejected = await client.PostAsJsonAsync(MarkerTestData.Path(game.Id),
				new { categoryId = destination, name = "Marker", position = new { x = 0, y = 0 } });

			Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
		}

		using var missingGame = await client.PostAsJsonAsync(MarkerTestData.Path(Guid.NewGuid()),
			new { categoryId = category.Id, name = "Marker", position = new { x = 0, y = 0 } });

		Assert.Equal(HttpStatusCode.NotFound, missingGame.StatusCode);
	}

	[Fact]
	public async Task CreationCannotOverrideServerOwnedMetadata()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsJsonAsync(MarkerTestData.Path(game.Id), new
		{
			categoryId = category.Id,
			name = "Marker",
			position = new { x = 0, y = 0 },
			id = Guid.Empty,
			gameId = Guid.Empty,
			createdAt = DateTimeOffset.MinValue,
			updatedAt = DateTimeOffset.MinValue
		});

		var dto = await MarkerTestData.ReadAsync(response);

		Assert.Equal(7, dto.GetProperty("id").GetGuid().Version);
		Assert.NotEqual(DateTimeOffset.MinValue, dto.GetProperty("createdAt").GetDateTimeOffset());
		Assert.Equal(dto.GetProperty("createdAt").GetDateTimeOffset(), dto.GetProperty("updatedAt").GetDateTimeOffset());
	}

	[Fact]
	public async Task TransfersReplacementClearingAndEveryCategoryResponseUseRealCounts()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var destinationGroup = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);

		var source = await CategoryTestData.SeedAsync(factory, group.Id, "Source");

		var destination = await CategoryTestData.SeedAsync(factory, group.Id, "Destination", 2);

		var (_, otherGroup) = await CategoryTestData.ParentAsync(factory);

		var foreignCategory = await CategoryTestData.SeedAsync(factory, otherGroup.Id);
		using var client = factory.CreateHttpsClient();

		var created = await MarkerTestData.CreateAsync(client, game.Id, source.Id,
			media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));

		var id = created.GetProperty("id").GetGuid();

		var path = MarkerTestData.Path(game.Id) + $"/{id}";

		foreach (var invalid in new[] { Guid.Empty, Guid.NewGuid(), foreignCategory.Id })
		{
			using var failed = await client.PatchAsJsonAsync(path,
				new
				{
					categoryId = invalid,
					name = "Should not persist",
					position = new { x = 123 },
					media = Array.Empty<object>()
				});

			Assert.Equal(HttpStatusCode.NotFound, failed.StatusCode);
			Assert.Equal("Marker", (await MarkerTestData.LoadAsync(factory, id)).Name);
		}

		var reversed = created.GetProperty("media").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Reverse().ToArray();
		using var moved = await client.PatchAsJsonAsync(path,
			new { categoryId = destination.Id, name = " Moved ", description = " New ", media = reversed });

		var updated = await MarkerTestData.ReadAsync(moved);

		var persisted = await MarkerTestData.LoadAsync(factory, id);

		Assert.Equal(destination.Id, persisted.CategoryId);
		Assert.Equal("Moved", persisted.Name);
		Assert.Equal("New", persisted.Description);
		Assert.Equal(new[] { "video", "image", "image" },
			updated.GetProperty("media").EnumerateArray().Select(item => item.GetProperty("type").GetString()));
		Assert.Equal(new[] { 0, 1, 2 }, persisted.Media.OrderBy(item => item.Order).Select(item => item.Order));

		var counts = System.Text.Json.JsonSerializer.SerializeToElement(await CategoryTestData.ReadTreeCategoriesAsync(client, CategoryTestData.Path(game.Id, group.Id)), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

		Assert.Equal(0, counts[0].GetProperty("markersCount").GetInt32());
		Assert.Equal(1, counts[1].GetProperty("markersCount").GetInt32());

		foreach (var patch in new object[]
			{ new { name = "Renamed" }, new { beforeId = (Guid?)null }, new { categoryGroupId = destinationGroup.Id } })
		{
			using var categoryPatch = await client.PatchAsJsonAsync(
				CategoryTestData.Path(game.Id, group.Id, destination.Id), patch);

			var categoryDto = await MarkerTestData.ReadAsync(categoryPatch);

			Assert.Equal(1, categoryDto.GetProperty("markersCount").GetInt32());
		}

		using var newCategoryResponse = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, destinationGroup.Id),
			new { name = "Empty", iconAssetId = MediaTestData.IconId, color = "#fff" });

		Assert.Equal(0, (await MarkerTestData.ReadAsync(newCategoryResponse)).GetProperty("markersCount").GetInt32());
		using var readOnly = await client.PatchAsJsonAsync(
			CategoryTestData.Path(game.Id, destinationGroup.Id, destination.Id), new { markersCount = 0 });

		Assert.Equal(HttpStatusCode.BadRequest, readOnly.StatusCode);
		using var clear = await client.PatchAsJsonAsync(path, new { media = Array.Empty<object>(), description = " " });

		var cleared = await MarkerTestData.ReadAsync(clear);

		Assert.Empty(cleared.GetProperty("media").EnumerateArray());
		Assert.Equal(JsonValueKind.Null, cleared.GetProperty("description").ValueKind);
		await using (var scope = factory.Services.CreateAsyncScope())
			Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
				.Set<MarkerAsset>().CountAsync());

		using var deleted = await client.DeleteAsync(path);

		var afterDelete = System.Text.Json.JsonSerializer.SerializeToElement(await CategoryTestData.ReadTreeCategoriesAsync(client, CategoryTestData.Path(game.Id, destinationGroup.Id)), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.All(afterDelete.EnumerateArray(), item => Assert.Equal(0, item.GetProperty("markersCount").GetInt32()));
		Assert.Equal(source.UpdatedAt, (await CategoryTestData.LoadAsync(factory, group.Id)).Single().UpdatedAt);
	}

	[Theory]
	[InlineData("marker")]
	[InlineData("category")]
	[InlineData("group")]
	[InlineData("game")]
	public async Task PhysicalCascadesRemoveOnlyCurrentOwnershipAndNeverFiles(string resource)
	{
		var referencedFile = System.IO.Path.GetTempFileName();

		try
		{
			await using var factory = new GameApiFactory();

			await factory.MigrateAsync();
			await MediaTestData.EnsureMarkerAssetsAsync(factory);

			var (game, group) = await CategoryTestData.ParentAsync(factory);

			var category = await CategoryTestData.SeedAsync(factory, group.Id);

			var (otherGame, otherGroup) = await CategoryTestData.ParentAsync(factory);

			var otherCategory = await CategoryTestData.SeedAsync(factory, otherGroup.Id);
			using var client = factory.CreateHttpsClient();

			var original = System.IO.Path.Combine(factory.MediaStoragePath, "assets", MediaTestData.VideoId.ToString("N"));

			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(original)!);
			await File.WriteAllTextAsync(original, "original video bytes");

			var created = await MarkerTestData.CreateAsync(client, game.Id, category.Id,
				media: JsonSerializer.SerializeToElement(new[] { MediaTestData.VideoId }));

			var survivor = await MarkerTestData.CreateAsync(client, otherGame.Id, otherCategory.Id,
				media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));

			var path = resource switch
			{
				"marker" => MarkerTestData.Path(game.Id) + "/" + created.GetProperty("id").GetGuid(),
				"category" => CategoryTestData.Path(game.Id, group.Id, category.Id),
				"group" => $"/games/{game.Id}/maps/{MapTestData.MapIdFor(game.Id)}/category-groups/{group.Id}",
				_ => $"/games/{game.Id}"
			};

			using var deleted = await client.DeleteAsync(path);

			Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
			await using var scope = factory.Services.CreateAsyncScope();

			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			Assert.Equal(survivor.GetProperty("id").GetGuid(), (await db.Markers.SingleAsync()).Id);
			Assert.Equal(3, await db.Set<MarkerAsset>().CountAsync());
			Assert.True(File.Exists(referencedFile));
			Assert.Equal("original video bytes", await File.ReadAllTextAsync(original));
			Assert.True(await db.MediaAssets.AnyAsync(asset => asset.Id == MediaTestData.VideoId));
		}
		finally
		{
			File.Delete(referencedFile);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TransfersSurviveFormerParentDeletion(bool moveCategory)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);

		var (game, sourceGroup) = await CategoryTestData.ParentAsync(factory);

		var destinationGroup = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);

		var sourceCategory = await CategoryTestData.SeedAsync(factory, sourceGroup.Id, "Source");

		var destinationCategory = await CategoryTestData.SeedAsync(factory, destinationGroup.Id, "Destination");
		using var client = factory.CreateHttpsClient();

		var created = await MarkerTestData.CreateAsync(client, game.Id, sourceCategory.Id,
			media: JsonSerializer.Deserialize<JsonElement>(MarkerTestData.MixedMedia));

		var id = created.GetProperty("id").GetGuid();
		using var moved = moveCategory
			? await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, sourceGroup.Id, sourceCategory.Id),
				new { categoryGroupId = destinationGroup.Id })
			: await client.PatchAsJsonAsync(MarkerTestData.Path(game.Id) + $"/{id}",
				new { categoryId = destinationCategory.Id });

		Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
		using var deleted = await client.DeleteAsync($"/games/{game.Id}/maps/{MapTestData.MapIdFor(game.Id)}/category-groups/{sourceGroup.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

		var survivor = await MarkerTestData.LoadAsync(factory, id);

		Assert.Equal(moveCategory ? sourceCategory.Id : destinationCategory.Id, survivor.CategoryId);
		Assert.Equal(3, survivor.Media.Count);
	}
}
