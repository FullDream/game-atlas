using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("GameAtlas");

		if (string.IsNullOrWhiteSpace(connectionString))
			throw new InvalidOperationException("Connection string 'GameAtlas' is required.");

		services.AddDbContext<ApplicationDbContext>(options => options
			.UseSqlite(connectionString, sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
			.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

		services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
		services.AddSingleton<IPersistenceFailureClassifier, SqlitePersistenceFailureClassifier>();

		return services;
	}
}
