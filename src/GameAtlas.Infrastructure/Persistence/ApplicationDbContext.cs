using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.Games;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: DbContext(options), IApplicationDbContext
{
	public DbSet<Game> Games => Set<Game>();

	protected override void OnModelCreating(ModelBuilder modelBuilder) =>
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}
