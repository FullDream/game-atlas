using System.Net;
using System.Net.Http.Json;
using GameAtlas.Domain.Categories;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CategoryPersistenceTests
{
	[Fact]
	public async Task MigrationUpgradeRoundTripAndRollbackPreserveParents()
	{
		await using var factory = new GameApiFactory();

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await context.GetService<IMigrator>().MigrateAsync("20261023100000_CategoryGroups");
		}

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id, "Ресурсы", 4);
		var dto = Assert.Single(await CategoryTestData.LoadAsync(factory, group.Id));

		Assert.Equal(category.Id, dto.Id);
		Assert.Equal(category.CategoryGroupId, dto.CategoryGroupId);
		Assert.Equal(category.Name, dto.Name);
		Assert.Equal(category.Icon, dto.Icon);
		Assert.Equal(category.Color, dto.Color);
		Assert.Equal(category.Order, dto.Order);
		Assert.Equal(category.CreatedAt, dto.CreatedAt);
		Assert.Equal(category.UpdatedAt, dto.UpdatedAt);
		await using var reload = factory.Services.CreateAsyncScope();
		var reloaded = reload.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var entityType = reloaded.Model.FindEntityType(typeof(Category));

		Assert.NotNull(entityType);
		Assert.Null(entityType.FindProperty("MarkersCount"));
		Assert.False(reloaded.Database.HasPendingModelChanges());
		var rollback = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
			reloaded.GetService<IMigrator>().MigrateAsync("20261023100000_CategoryGroups"));

		Assert.Contains("MapOwnership_RestoreBackupForNonemptyMaps", rollback.Message);

		DeleteGameHandlerTests.AssertSameGame(game, await reloaded.Games.SingleAsync());
		Assert.Equal(group.Id, (await reloaded.CategoryGroups.SingleAsync()).Id);
	}

	[Theory]
	[InlineData("Resources", " resources ")]
	[InlineData("Ресурсы", " ресурсы ")]
	public async Task UniqueConstraintAndForeignKeyArePreciselyClassified(string name, string duplicate)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var other = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Other", 2);

		await CategoryTestData.SeedAsync(factory, group.Id, name);
		await CategoryTestData.SeedAsync(factory, other.Id, duplicate);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var classifier = new SqlitePersistenceFailureClassifier();

		context.Categories.Add(Assert.IsType<Category>(
			Category.Create(group.Id, duplicate, "icon", "#123456", 1).Value));

		var conflict = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		Assert.True(classifier.IsCategoryNameConflict(conflict));
		Assert.False(classifier.IsCategoryGroupNameConflict(conflict));
		Assert.False(classifier.IsGameSlugConflict(conflict));
		Assert.False(classifier.IsCategoryNameConflict(new InvalidOperationException("private failure")));
		context.ChangeTracker.Clear();
		context.Categories.Add(Assert.IsType<Category>(
			Category.Create(Guid.NewGuid(), "Orphan", "icon", "#123456", 1).Value));

		var foreignKey = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		Assert.False(classifier.IsCategoryNameConflict(foreignKey));
		context.ChangeTracker.Clear();
		context.CategoryGroups.Add(Assert.IsType<GameAtlas.Domain.CategoryGroups.CategoryGroup>(
			GameAtlas.Domain.CategoryGroups.CategoryGroup.Create(MapTestData.MapIdFor(game.Id), "Group", 2).Value));
		var groupConflict = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		Assert.False(classifier.IsCategoryNameConflict(groupConflict));
		Assert.True(classifier.IsCategoryGroupNameConflict(groupConflict));
		context.ChangeTracker.Clear();
		context.Games.Add(Assert.IsType<GameAtlas.Domain.Games.Game>(
			GameAtlas.Domain.Games.Game.Create("Copy", game.Slug, "image").Value));
		var gameConflict = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		Assert.False(classifier.IsCategoryNameConflict(gameConflict));
		Assert.True(classifier.IsGameSlugConflict(gameConflict));
		Assert.Single(await CategoryTestData.LoadAsync(factory, group.Id));
		Assert.Single(await CategoryTestData.LoadAsync(factory, other.Id));
	}

	[Fact]
	public async Task GroupAndGameDeletionCascadeAccordingToCurrentMembership()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var (game, source) = await CategoryTestData.ParentAsync(factory);
		var destination = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);
		var (otherGame, otherGroup) = await CategoryTestData.ParentAsync(factory);
		var moved = await CategoryTestData.SeedAsync(factory, source.Id, "Moved");

		await CategoryTestData.SeedAsync(factory, source.Id, "Deleted");
		await CategoryTestData.SeedAsync(factory, destination.Id, "Retained");
		await CategoryTestData.SeedAsync(factory, otherGroup.Id, "Unrelated");
		var unrelated = await CategoryTestData.LoadAsync(factory, otherGroup.Id);
		using var client = factory.CreateHttpsClient();
		using var transfer = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, source.Id, moved.Id),
			new { categoryGroupId = destination.Id });

		Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);
		using var deleteSource = await client.DeleteAsync($"/games/{game.Id}/maps/{MapTestData.MapIdFor(game.Id)}/category-groups/{source.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleteSource.StatusCode);
		Assert.Empty(await CategoryTestData.LoadAsync(factory, source.Id));
		Assert.Equal(2, (await CategoryTestData.LoadAsync(factory, destination.Id)).Length);
		using var deleteGame = await client.DeleteAsync($"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleteGame.StatusCode);
		Assert.Empty(await CategoryTestData.LoadAsync(factory, destination.Id));
		Assert.Equal(unrelated, await CategoryTestData.LoadAsync(factory, otherGroup.Id));
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Equal(otherGame.Id, (await context.Games.SingleAsync()).Id);
		Assert.Equal(otherGroup.Id, (await context.CategoryGroups.SingleAsync()).Id);
	}
}
