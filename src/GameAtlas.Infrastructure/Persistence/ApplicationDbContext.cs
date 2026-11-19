using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.Media;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: IdentityUserContext<IdentityUser>(options), IApplicationDbContext
{
	public DbSet<Game> Games => Set<Game>();
	public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();
	public DbSet<Category> Categories => Set<Category>();
	public DbSet<Marker> Markers => Set<Marker>();
	public DbSet<Map> Maps => Set<Map>();
	public DbSet<MapStyle> MapStyles => Set<MapStyle>();
	public DbSet<MediaFolder> MediaFolders => Set<MediaFolder>();
	public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

	public async Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
		new ApplicationTransaction(await Database.BeginTransactionAsync(cancellationToken));

	public Task LockMapAsync(Guid mapId, CancellationToken cancellationToken = default) =>
		Database.ExecuteSqlAsync($"""SELECT 1 FROM "Maps" WHERE "Id" = {mapId} FOR UPDATE""",
			cancellationToken);

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
	}

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		AlignTimestamps();

		return base.SaveChanges(acceptAllChangesOnSuccess);
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
		CancellationToken cancellationToken = default)
	{
		AlignTimestamps();

		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	private void AlignTimestamps()
	{
		foreach (var entry in ChangeTracker.Entries())
		{
			if (entry.State is not (EntityState.Added or EntityState.Modified))
				continue;

			foreach (var property in entry.Properties)
			{
				if (property.CurrentValue is DateTimeOffset timestamp)
				{
					var ticks = timestamp.Ticks - (timestamp.Ticks % TimeSpan.TicksPerMicrosecond);

					if (ticks != timestamp.Ticks)
						property.CurrentValue = new DateTimeOffset(ticks, timestamp.Offset);
				}
			}
		}
	}
}
