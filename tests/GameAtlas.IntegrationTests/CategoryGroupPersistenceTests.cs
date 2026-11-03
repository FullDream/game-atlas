using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CategoryGroupPersistenceTests
{
	[Fact]
	public async Task MigrationPersistsGroupsAcrossConnections()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();

		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");

		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, " Ресурсы ", 4);

		var stored = Assert.Single(await CategoryGroupTestData.LoadAsync(factory, game.Id));

		CategoryGroupTestData.AssertSame(group.Id, stored.Id);
		CategoryGroupTestData.AssertSame(MapTestData.MapIdFor(game.Id), stored.MapId);
		CategoryGroupTestData.AssertSame("Ресурсы", stored.Name);
		CategoryGroupTestData.AssertSame(4, await CategoryGroupTestData.OrderAsync(factory, stored.Id));
		CategoryGroupTestData.AssertSame(group.CreatedAt, stored.CreatedAt);
		CategoryGroupTestData.AssertSame(group.UpdatedAt, stored.UpdatedAt);
		await using var reload = factory.Services.CreateAsyncScope();

		var contextAfter = reload.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var storedGame = await contextAfter.Games.SingleAsync();

		DeleteGameHandlerTests.AssertSameGame(game, storedGame);
		Assert.False(contextAfter.Database.HasPendingModelChanges());
	}

	[Theory]
	[InlineData("Resources", " resources ")]
	[InlineData("Ресурсы", " ресурсы ")]
	public async Task NameConstraintIsScopedAndPreciselyClassified(string name, string duplicate)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		await MapTestData.EnsureMapAsync(factory, game.Id);

		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");

		await CategoryGroupTestData.SeedAsync(factory, game.Id, name, 1);
		await CategoryGroupTestData.SeedAsync(factory, other.Id, duplicate, 1);
		await using var scope = factory.Services.CreateAsyncScope();

		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		context.CategoryGroups.Add(CategoryGroup.Create(MapTestData.MapIdFor(game.Id), duplicate, 1).Value!);

		var failure = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		var classifier = new SqlitePersistenceFailureClassifier();

		Assert.True(classifier.IsCategoryGroupNameConflict(failure));
		Assert.False(classifier.IsGameSlugConflict(failure));
		Assert.False(classifier.IsCategoryGroupNameConflict(new InvalidOperationException("private failure")));
		Assert.Single(await CategoryGroupTestData.LoadAsync(factory, game.Id));
		Assert.Single(await CategoryGroupTestData.LoadAsync(factory, other.Id));
	}

	[Fact]
	public async Task ForeignKeyRejectsOrphansAndGameDeleteCascadesWithoutLoadingChildren()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");

		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");

		await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		await CategoryGroupTestData.SeedAsync(factory, other.Id, "Group", 1);

		var before = await CategoryGroupTestData.LoadAsync(factory, other.Id);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			CategoryGroupTestData.AssertSame(1, await context.Games.Where(item => item.Id == game.Id).ExecuteDeleteAsync());
			Assert.Empty(context.ChangeTracker.Entries());
			context.CategoryGroups.Add(CategoryGroup.Create(Guid.NewGuid(), "Orphan", 1).Value!);

			var failure = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

			Assert.False(new SqlitePersistenceFailureClassifier().IsCategoryGroupNameConflict(failure));
		}

		Assert.Empty(await CategoryGroupTestData.LoadAsync(factory, game.Id));
		CategoryGroupTestData.AssertSame(before, await CategoryGroupTestData.LoadAsync(factory, other.Id));
	}
}
