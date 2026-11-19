using GameAtlas.Domain;
using GameAtlas.Domain.ValueObjects;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class GamePersistenceTests
{
	[Fact]
	public async Task MigrationPersistsMetadataAcrossConnectionsWithStringStatus()
	{
		await using var factory = new GameApiFactory();
		var game = Game.Create(" Game ", Slug.Create(" game ").Value!, MediaTestData.CoverId).Value!;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			await context.Database.MigrateAsync();
			await MediaTestData.EnsureCoverAsync(context);
			context.Games.Add(game);
			await context.SaveChangesAsync();
		}

		await using var reopenedScope = factory.Services.CreateAsyncScope();
		var reopened = reopenedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var stored = await reopened.Games.SingleAsync();

		Assert.Equal(game.Id, stored.Id);
		Assert.Equal(game.Name, stored.Name);
		Assert.Equal(game.Slug, stored.Slug);
		Assert.Equal(game.CoverId, stored.CoverId);
		Assert.Equal(game.CreatedAt, stored.CreatedAt);
		Assert.Equal(game.UpdatedAt, stored.UpdatedAt);
		Assert.Equal(PublicationStatus.Draft, stored.Status);
		await reopened.Database.OpenConnectionAsync();
		await using var command = reopened.Database.GetDbConnection().CreateCommand();

		command.CommandText = "SELECT \"Status\" FROM \"Games\"";
		Assert.Equal("Draft", await command.ExecuteScalarAsync());
	}

	[Fact]
	public async Task UniqueIndexRejectsDuplicateSlugButAllowsDuplicateName()
	{
		await using var factory = new GameApiFactory();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		await context.Database.MigrateAsync();
		await MediaTestData.EnsureCoverAsync(context);
		context.Games.AddRange(Game.Create("Game", Slug.Create("first").Value!, MediaTestData.CoverId).Value!,
			Game.Create("Game", Slug.Create("second").Value!, MediaTestData.CoverId).Value!);
		await context.SaveChangesAsync();
		context.Games.Add(Game.Create("Other", Slug.Create("first").Value!, MediaTestData.CoverId).Value!);
		await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
		Assert.Equal(2, await context.Games.CountAsync());
	}
}
