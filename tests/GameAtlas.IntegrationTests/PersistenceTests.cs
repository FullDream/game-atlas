using GameAtlas.Application;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure;
using GameAtlas.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class PersistenceTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ApplicationTransactionPersistsOnlyAfterExplicitCommit(bool commit)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var map = await MapTestData.EnsureMapAsync(factory, game.Id);

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			await using var transaction = await context.BeginTransactionAsync();
			var group = CategoryGroup.Create(map.Id, "Resources", 1);

			Assert.True(group.IsSuccess);
			context.CategoryGroups.Add(group.Value);
			await context.SaveChangesAsync();
			Assert.Equal(1, await context.CategoryGroups.CountAsync());

			if (commit)
				await transaction.CommitAsync();
		}

		var stored = await CategoryGroupTestData.LoadAsync(factory, game.Id);

		Assert.Equal(commit ? 1 : 0, stored.Length);
	}

	[Fact]
	public async Task RegisteredContextUsesSqliteAndSharesTheScopedInstance()
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:GameAtlas"] = "Data Source=:memory:"
			})
			.Build();
		var services = new ServiceCollection();

		services.AddLogging();
		services.AddApplication().AddInfrastructure(configuration);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateOnBuild = true,
			ValidateScopes = true
		});

		await using var scope = provider.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var applicationContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

		Assert.Same(context, applicationContext);
		Assert.NotNull(scope.ServiceProvider.GetRequiredService<IMediator>());
		Assert.True(context.Database.IsSqlite());
		var gameType = context.Model.FindEntityType(typeof(Game));

		Assert.NotNull(gameType);
		Assert.Equal(new[] { "BannerId", "CoverId" }, gameType.GetForeignKeys().Select(key => key.Properties.Single().Name).Order());
		Assert.Contains(gameType.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == "Slug");

		await context.Database.OpenConnectionAsync();
		await using var command = context.Database.GetDbConnection().CreateCommand();

		command.CommandText = "SELECT 1";
		Assert.Equal(1L, await command.ExecuteScalarAsync());
		Assert.Equal(0, await applicationContext.SaveChangesAsync());

		await using var anotherScope = provider.CreateAsyncScope();

		Assert.NotSame(context, anotherScope.ServiceProvider.GetRequiredService<IApplicationDbContext>());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public void MissingConnectionStringFailsDuringRegistration(string? connectionString)
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:GameAtlas"] = connectionString
			})
			.Build();

		var exception = Assert.Throws<InvalidOperationException>(() =>
			new ServiceCollection().AddInfrastructure(configuration));

		Assert.Contains("GameAtlas", exception.Message);
	}
}
