using GameAtlas.Domain.Maps;
using GameAtlas.Domain.ValueObjects;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapSlugMigrationTests
{
	[Fact]
	public async Task PopulatedUpgradeAndDowngradePreserveMetadataContentAndFiles()
	{
		await using var factory = new GameApiFactory();
		await factory.MigrateAsync();
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		var seeded = await MapTestData.SeedAsync(factory, game.Id);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var map = await context.Maps.AsTracking().Include(item => item.Styles).SingleAsync(item => item.Id == seeded.Id);
		var style = Assert.Single(map.Styles);
		style.Claim(style.AttemptId);
		style.Publish(style.AttemptId);
		map.ReconcileDefaultStyle();
		var blank = Map.Create(game.Id, "Карта", Slug.Create("old-blank").Value!).Value!;
		context.Maps.Add(blank);
		await context.SaveChangesAsync();
		var timestamp = map.UpdatedAt;
		var tilePath = Path.Combine(factory.MapStoragePath, "tiles", map.Id.ToString("N"), style.Id.ToString("N"), "0", "0", "0.webp");
		Directory.CreateDirectory(Path.GetDirectoryName(tilePath)!);
		await File.WriteAllBytesAsync(tilePath, [1, 2, 3]);
		var migrator = context.GetService<IMigrator>();

		await migrator.MigrateAsync("20261105152741_OptionalCategoryColor");
		Assert.Equal(2, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Maps").SingleAsync());
		await migrator.MigrateAsync();
		context.ChangeTracker.Clear();
		var restored = await context.Maps.Include(item => item.Styles).SingleAsync(item => item.Id == map.Id);
		var restoredBlank = await context.Maps.SingleAsync(item => item.Id == blank.Id);

		Assert.Equal($"map-{map.Id:N}", restored.Slug);
		Assert.Equal($"map-{blank.Id:N}", restoredBlank.Slug);
		Assert.Equal("Карта", restoredBlank.Name);
		Assert.Equal(blank.UpdatedAt, restoredBlank.UpdatedAt);
		Assert.Null(restoredBlank.Geometry);
		Assert.Equal(map.GameId, restored.GameId);
		Assert.Equal(map.Name, restored.Name);
		Assert.Equal(map.CreatedAt, restored.CreatedAt);
		Assert.Equal(timestamp, restored.UpdatedAt);
		Assert.Equal(map.Geometry, restored.Geometry);
		Assert.Equal(style.Id, restored.DefaultStyleId);
		var restoredStyle = Assert.Single(restored.Styles);

		Assert.Equal((style.Id, style.AttemptId, style.Status, style.SourceAssetId, style.UpdatedAt),
			(restoredStyle.Id, restoredStyle.AttemptId, restoredStyle.Status, restoredStyle.SourceAssetId, restoredStyle.UpdatedAt));
		Assert.Equal(category.Id, (await context.Categories.SingleAsync()).Id);
		Assert.Equal(group.Id, (await context.CategoryGroups.SingleAsync()).Id);
		Assert.Equal(game.Slug, (await context.Games.SingleAsync()).Slug);
		Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(tilePath));
		Assert.DoesNotContain(await context.Database.SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE name = 'Maps'").SingleAsync(),
			"DEFAULT ''", StringComparison.OrdinalIgnoreCase);
	}
}
