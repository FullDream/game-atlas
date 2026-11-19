using GameAtlas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.Infrastructure.Identity;

public static class AdministratorBootstrap
{
	public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
	{
		await using var scope = services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		if (await db.Users.AnyAsync())
			return;

		await using var transaction = await db.Database.BeginTransactionAsync();

		if (await db.Users.AnyAsync())
			return;

		var login = configuration["Admin:Login"];
		var password = configuration["Admin:Password"];

		if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
			throw new InvalidOperationException("Set Admin:Login and Admin:Password to provision the first administrator.");

		var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
		var result = await manager.CreateAsync(new IdentityUser(login), password);

		if (!result.Succeeded)
			throw new InvalidOperationException("Administrator provisioning failed: " + string.Join(", ", result.Errors.Select(e => e.Code)));

		await transaction.CommitAsync();
	}
}
