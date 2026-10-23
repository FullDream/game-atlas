using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Domain.Markers;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: DbContext(options), IApplicationDbContext
{
	public DbSet<Game> Games => Set<Game>();
	public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();
	public DbSet<Category> Categories => Set<Category>();
	public DbSet<Marker> Markers => Set<Marker>();

	public async Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
		new ApplicationTransaction(await Database.BeginTransactionAsync(cancellationToken));

	protected override void OnModelCreating(ModelBuilder modelBuilder) =>
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}
