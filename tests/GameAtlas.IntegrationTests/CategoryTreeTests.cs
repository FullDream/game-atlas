using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Categories;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CategoryTreeTests
{
	[Fact]
	public async Task GroupColorsRoundTripNormalizeAndRejectNullAtomically()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var (game, parent) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var created = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "Colored", color = "#abc" });
		Assert.Equal(HttpStatusCode.OK, created.StatusCode);
		var group = (await created.Content.ReadFromJsonAsync<CategoryGroupDto>())!;
		Assert.Equal("#AABBCC", group.Color);
		using var changed = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, group.Id), new { color = "#123456" });
		Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
		var updated = (await changed.Content.ReadFromJsonAsync<CategoryGroupDto>())!;
		Assert.Equal("#123456", updated.Color);
		using var noOp = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, group.Id), new { color = "#123456" });
		Assert.Equal(updated.UpdatedAt, (await noOp.Content.ReadFromJsonAsync<CategoryGroupDto>())!.UpdatedAt);
		foreach (var color in new string?[] { null, "", "red", "#12", "#fff\n" })
		{
			using var rejected = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, group.Id), new { name = "Must not persist", color });
			Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
			using var rejectedCreate = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id), new { name = "Invalid color", color });
			Assert.Equal(HttpStatusCode.UnprocessableEntity, rejectedCreate.StatusCode);
		}
		var tree = (await client.GetFromJsonAsync<CategoryGroupDto[]>(CategoryGroupApiTests.Path(game.Id)))!;
		Assert.Equal("#F59E0B", tree.Single(item => item.Id == parent.Id).Color);
		Assert.Equal("Colored", tree.Single(item => item.Id == group.Id).Name);
		Assert.Equal("#123456", tree.Single(item => item.Id == group.Id).Color);
	}

	[Fact]
	public async Task GroupColorMigrationBackfillsExistingGroupsWithoutChangingMetadata()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var (_, group) = await CategoryTestData.ParentAsync(factory);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var migrator = context.GetService<IMigrator>();
		await migrator.MigrateAsync("20261101172032_CategoryTree");
		await migrator.MigrateAsync();
		context.ChangeTracker.Clear();
		var stored = await context.CategoryGroups.SingleAsync(item => item.Id == group.Id);
		Assert.Equal("#F59E0B", stored.Color);
		Assert.Equal(group.Name, stored.Name);
		Assert.Equal(group.CreatedAt, stored.CreatedAt);
		Assert.Equal(group.UpdatedAt, stored.UpdatedAt);
	}

	[Fact]
	public async Task OptionalIconsAndCountedTreeSurviveTransferAndClear()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var (game, source) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();

		var groupIcon = await MediaTestData.UploadImageAsync(client, width: 93, height: 57);
		using var groupResponse = await client.PostAsJsonAsync(CategoryGroupApiTests.Path(game.Id),
			new { name = "Destination", iconAssetId = groupIcon.Id });

		var destination = (await groupResponse.Content.ReadFromJsonAsync<CategoryGroupDto>())!;
		using var created = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, source.Id),
			new { name = "Iconless", color = "#abc" });

		var category = await CategoryApiTests.ReadAsync(created);

		Assert.Null(category.Icon);
		await MarkerTestData.CreateAsync(client, game.Id, category.Id);
		using var transfer = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, source.Id, category.Id),
			new { categoryGroupId = destination.Id, beforeId = (Guid?)null });
		Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

		var groups = (await client.GetFromJsonAsync<CategoryGroupDto[]>(CategoryGroupApiTests.Path(game.Id)))!;

		Assert.Equal(0, groups.Single(group => group.Id == source.Id).MarkersCount);

		var movedGroup = groups.Single(group => group.Id == destination.Id);
		Assert.Equal(1, movedGroup.MarkersCount);
		Assert.Equal(category.Id, Assert.Single(movedGroup.Categories).Id);
		Assert.Null(movedGroup.Categories.Single().Icon);
		Assert.Equal(groupIcon.Id, movedGroup.Icon!.Id);
		using var guarded = await client.DeleteAsync("/media/assets/" + groupIcon.Id);
		Assert.Equal(HttpStatusCode.Conflict, guarded.StatusCode);
		using var clear = await client.PatchAsJsonAsync(CategoryGroupApiTests.Path(game.Id, destination.Id),
			new { iconAssetId = (Guid?)null });
		Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
		Assert.Null((await clear.Content.ReadFromJsonAsync<CategoryGroupDto>())!.Icon);
		using var attach = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, destination.Id, category.Id), new { iconAssetId = groupIcon.Id });
		Assert.NotNull((await CategoryApiTests.ReadAsync(attach)).Icon);
		using var detach = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, destination.Id, category.Id), new { iconAssetId = (Guid?)null });
		Assert.Null((await CategoryApiTests.ReadAsync(detach)).Icon);
		using var released = await client.DeleteAsync("/media/assets/" + groupIcon.Id);
		Assert.Equal(HttpStatusCode.NoContent, released.StatusCode);
		using var removedGet = await client.GetAsync(CategoryTestData.Path(game.Id, source.Id));
		Assert.Equal(HttpStatusCode.MethodNotAllowed, removedGet.StatusCode);
	}

	[Fact]
	public async Task RelativeMovesPreserveSiblingMetadataAndRejectInvalidAnchorsAtomically()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var first = await CategoryTestData.SeedAsync(factory, group.Id, "First", 1);

		var second = await CategoryTestData.SeedAsync(factory, group.Id, "Second", 2);

		var third = await CategoryTestData.SeedAsync(factory, group.Id, "Third", 3);
		using var client = factory.CreateHttpsClient();
		using var moved = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, third.Id), new { beforeId = first.Id });
		Assert.Equal(HttpStatusCode.OK, moved.StatusCode);

		var items = await CategoryTestData.LoadAsync(factory, group.Id);
		Assert.Equal(new[] { third.Id, first.Id, second.Id }, items.Select(item => item.Id));
		Assert.Equal(new[] { 1, 2, 3 }, await Task.WhenAll(items.Select(item => CategoryTestData.OrderAsync(factory, item.Id))));
		Assert.Equal(first.UpdatedAt, items.Single(item => item.Id == first.Id).UpdatedAt);
		using var noOp = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, third.Id), new { beforeId = first.Id });
		Assert.Equal(items[0].UpdatedAt, (await CategoryApiTests.ReadAsync(noOp)).UpdatedAt);
		using var self = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, third.Id), new { name = "Rejected", beforeId = third.Id });
		Assert.Equal(HttpStatusCode.UnprocessableEntity, self.StatusCode);
		using var missing = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, third.Id), new { beforeId = Guid.NewGuid() });
		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		Assert.Equal(items, await CategoryTestData.LoadAsync(factory, group.Id));
	}

	[Fact]
	public async Task TransferBeforeDestinationSiblingAndCreationPlacementAreAtomic()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var (game, source) = await CategoryTestData.ParentAsync(factory);

		var destination = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);

		var moving = await CategoryTestData.SeedAsync(factory, source.Id, "Moving", 1);

		var anchor = await CategoryTestData.SeedAsync(factory, destination.Id, "Anchor", 1);
		using var client = factory.CreateHttpsClient();
		using var moved = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, source.Id, moving.Id),
			new { categoryGroupId = destination.Id, beforeId = anchor.Id });
		Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
		using var created = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, destination.Id),
			new { name = "Inserted", color = "#fff", beforeId = anchor.Id });

		var inserted = await CategoryApiTests.ReadAsync(created);

		var rows = await CategoryTestData.LoadAsync(factory, destination.Id);
		Assert.Equal(new[] { moving.Id, inserted.Id, anchor.Id }, rows.Select(item => item.Id));
		Assert.Empty(await CategoryTestData.LoadAsync(factory, source.Id));
		using var foreignAnchor = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, destination.Id, moving.Id),
			new { categoryGroupId = source.Id, beforeId = anchor.Id });
		Assert.Equal(HttpStatusCode.NotFound, foreignAnchor.StatusCode);
		Assert.Equal(rows, await CategoryTestData.LoadAsync(factory, destination.Id));
	}

	[Fact]
	public async Task ForwardMigrationNormalizesLegacyOrderWithoutChangingIdentityOrTimestamps()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var first = await CategoryTestData.SeedAsync(factory, group.Id, "First", 2);

		var tied = await CategoryTestData.SeedAsync(factory, group.Id, "Tied", 2);

		var last = await CategoryTestData.SeedAsync(factory, group.Id, "Last", int.MaxValue);

		var tiedGroup = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Tied group", 1);

		var lastGroup = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Last group", int.MaxValue);
		await using var scope = factory.Services.CreateAsyncScope();

		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var migrator = context.GetService<IMigrator>();
		await migrator.MigrateAsync("20261101112354_MapStylePresentation");
		await migrator.MigrateAsync();
		context.ChangeTracker.Clear();

		var rows = await context.Categories.OrderBy(category => category.Order).ToArrayAsync();
		Assert.Equal(new[] { first.Id, tied.Id, last.Id }, rows.Select(category => category.Id));
		Assert.Equal(new[] { 1, 2, 3 }, rows.Select(category => category.Order));
		Assert.Equal(first.UpdatedAt, rows[0].UpdatedAt);
		Assert.Equal(last.CreatedAt, rows[2].CreatedAt);

		var groups = await context.CategoryGroups.OrderBy(item => item.Order).ToArrayAsync();
		Assert.Equal(new[] { group.Id, tiedGroup.Id, lastGroup.Id }, groups.Select(item => item.Id));
		Assert.Equal(tiedGroup.UpdatedAt, groups[1].UpdatedAt);
	}

	[Fact]
	public async Task IconlessCategoryBlocksUnsafeDowngradeWithoutLosingData()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var created = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, group.Id), new { name = "No icon", color = "#fff" });

		var category = await CategoryApiTests.ReadAsync(created);
		await using var scope = factory.Services.CreateAsyncScope();

		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var error = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.GetService<IMigrator>().MigrateAsync("20261101112354_MapStylePresentation"));
		Assert.Contains("CategoryTree_RequiresCategoryIcons", error.Message);
		Assert.True(await context.Categories.AnyAsync(item => item.Id == category.Id));
		Assert.Contains("20261101172032_CategoryTree", await context.Database.GetAppliedMigrationsAsync());
	}

	[Fact]
	public async Task ConcurrentTransfersAndDeletionRetainEverySurvivorExactlyOnce()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var (game, source) = await CategoryTestData.ParentAsync(factory);

		var destination = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);

		var first = await CategoryTestData.SeedAsync(factory, source.Id, "First", 1);

		var second = await CategoryTestData.SeedAsync(factory, source.Id, "Second", 2);

		var removed = await CategoryTestData.SeedAsync(factory, source.Id, "Removed", 3);
		using var client = factory.CreateHttpsClient();

		var responses = await Task.WhenAll(
			client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, source.Id, first.Id), new { categoryGroupId = destination.Id }),
			client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, source.Id, second.Id), new { categoryGroupId = destination.Id }),
			client.DeleteAsync(CategoryTestData.Path(game.Id, source.Id, removed.Id)));

		try
		{
			Assert.Equal(HttpStatusCode.OK, responses[0].StatusCode);
			Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode);
			Assert.Equal(HttpStatusCode.NoContent, responses[2].StatusCode);

			var groups = (await client.GetFromJsonAsync<CategoryGroupDto[]>(CategoryGroupApiTests.Path(game.Id)))!;
			Assert.Empty(groups.Single(group => group.Id == source.Id).Categories);

			var children = groups.Single(group => group.Id == destination.Id).Categories;
			Assert.Equal(new[] { first.Id, second.Id }.Order(), children.Select(item => item.Id).Order());
			Assert.Equal(new[] { 1, 2 }, await Task.WhenAll(children.Select(item => CategoryTestData.OrderAsync(factory, item.Id))));
		}
		finally
		{
			foreach (var response in responses)
				response.Dispose();
		}
	}

	[Fact]
	public async Task ChangedSequenceUpdatesMovedTimestampEvenWhenNumericOrderIsUnchanged()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		await CategoryTestData.SeedAsync(factory, group.Id, "First", 1);

		var moving = await CategoryTestData.SeedAsync(factory, group.Id, "Moving", 3);

		var last = await CategoryTestData.SeedAsync(factory, group.Id, "Last", 4);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, moving.Id), new { beforeId = (Guid?)null });

		var moved = await CategoryApiTests.ReadAsync(response);
		Assert.Equal(3, await CategoryTestData.OrderAsync(factory, moved.Id));
		Assert.True(moved.UpdatedAt > moving.UpdatedAt);

		var items = await CategoryTestData.LoadAsync(factory, group.Id);
		Assert.Equal(moving.Id, items[^1].Id);
		Assert.Equal(last.UpdatedAt, items.Single(item => item.Id == last.Id).UpdatedAt);
	}
}
