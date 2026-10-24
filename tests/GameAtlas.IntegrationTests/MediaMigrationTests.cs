using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MediaMigrationTests
{
	[Fact]
	public async Task FreshFullChainHasNoModelDriftAndEmptyStoreCanRollBackAndUpgradeAgain()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.False(context.Database.HasPendingModelChanges());
		Assert.Equal(0, await context.Database.SqlQueryRaw<int>("""
			SELECT COUNT(*) AS Value FROM sqlite_master
			WHERE type = 'table' AND name IN ('MediaAdmissionSessions', 'MediaFileLeases', 'MediaCleanupIntents',
			    '__MediaCategoryIcons', '__MediaMarkerAssets', '__MediaMapStyleSources')
			""").SingleAsync());

		Assert.Equal(0, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").SingleAsync());
		await context.GetService<IMigrator>().MigrateAsync("0");

		Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
		await context.Database.MigrateAsync();

		Assert.False(await context.MediaAssets.AnyAsync());
		Assert.False(await context.MediaFolders.AnyAsync());
		Assert.Equal(0, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").SingleAsync());
	}
}
