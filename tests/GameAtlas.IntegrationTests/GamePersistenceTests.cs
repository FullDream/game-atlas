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
		var game = Game.Create(" Game ", " game ", MediaTestData.CoverId).Value!;

		await using (var context = CreateContext())
		{
			await context.Database.MigrateAsync();
			await MediaTestData.EnsureCoverAsync(context);
			context.Games.Add(game);
			await context.SaveChangesAsync();
		}

		await using var reopened = CreateContext();
		var stored = await reopened.Games.SingleAsync();

		Assert.Equal(game.Id, stored.Id);
		Assert.Equal(game.Name, stored.Name);
		Assert.Equal(game.Slug, stored.Slug);
		Assert.Equal(game.ImageAssetId, stored.ImageAssetId);
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
		await MediaTestData.EnsureCoverAsync(context);
		context.Games.AddRange(Game.Create("Game", "first", MediaTestData.CoverId).Value!,
			Game.Create("Game", "second", MediaTestData.CoverId).Value!);
		await context.SaveChangesAsync();
		context.Games.Add(Game.Create("Other", "first", MediaTestData.CoverId).Value!);
		await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
		Assert.Equal(2, await context.Games.CountAsync());
	}

	public void Dispose() => File.Delete(databasePath);
}
