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
			SELECT COUNT(*)::int AS "Value" FROM information_schema.tables
			WHERE table_schema = current_schema() AND table_name IN ('MediaAdmissionSessions', 'MediaFileLeases', 'MediaCleanupIntents',
			    '__MediaCategoryIcons', '__MediaMarkerAssets', '__MediaMapStyleSources')
			""").SingleAsync());

		await context.GetService<IMigrator>().MigrateAsync("0");

		Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
		await context.Database.MigrateAsync();

		Assert.False(await context.MediaAssets.AnyAsync());
		Assert.False(await context.MediaFolders.AnyAsync());
	}
}
