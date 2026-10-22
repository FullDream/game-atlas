using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameAtlas.IntegrationTests;

public sealed class GamePersistenceTests : IDisposable
{
	private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"game-atlas-{Guid.NewGuid():N}.db");

	private ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
		.UseSqlite($"Data Source={databasePath};Pooling=False").Options);

	[Fact]
	public async Task MigrationPersistsMetadataAcrossConnectionsWithStringStatus()
	{
		var game = Game.Create(" Game ", " game ", " cover ", Guid.NewGuid()).Value!;

		await using (var context = CreateContext())
		{
			await context.Database.MigrateAsync();
			context.Games.Add(game);
			await context.SaveChangesAsync();
		}

		await using var reopened = CreateContext();
		var stored = await reopened.Games.SingleAsync();

		Assert.Equal(game.Id, stored.Id);
		Assert.Equal(game.Name, stored.Name);
		Assert.Equal(game.Slug, stored.Slug);
		Assert.Equal(game.Image, stored.Image);
		Assert.Equal(game.MapId, stored.MapId);
		Assert.Equal(game.CreatedAt, stored.CreatedAt);
		Assert.Equal(game.UpdatedAt, stored.UpdatedAt);
		Assert.Equal(GameStatus.Draft, stored.Status);
		await reopened.Database.OpenConnectionAsync();
		await using var command = reopened.Database.GetDbConnection().CreateCommand();

		command.CommandText = "SELECT Status FROM Games";
		Assert.Equal("Draft", await command.ExecuteScalarAsync());
	}

	[Fact]
	public async Task UniqueIndexRejectsDuplicateSlugButAllowsDuplicateName()
	{
		await using var context = CreateContext();

		await context.Database.MigrateAsync();
		context.Games.AddRange(Game.Create("Game", "first", "image").Value!, Game.Create("Game", "second", "image").Value!);
		await context.SaveChangesAsync();
		context.Games.Add(Game.Create("Other", "first", "image").Value!);
		await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
		Assert.Equal(2, await context.Games.CountAsync());
	}

	public void Dispose() => File.Delete(databasePath);
}
