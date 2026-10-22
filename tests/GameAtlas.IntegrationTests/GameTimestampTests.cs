using System.Globalization;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameAtlas.IntegrationTests;

public class GameTimestampTests
{
	[Theory]
	[InlineData("createdAt", false)]
	[InlineData("createdAt", true)]
	[InlineData("updatedAt", false)]
	[InlineData("updatedAt", true)]
	public async Task OrdersOriginalTimestampTextInSqlWithoutLosingPrecision(string field, bool descending)
	{
		await using var connection = new SqliteConnection("Data Source=:memory:");

		await connection.OpenAsync();
		await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseSqlite(connection).Options);

		await context.GetService<IMigrator>().MigrateAsync("20261022182530_InitialGames");
		var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var instants = new[] { start, start.AddTicks(1), start.AddTicks(2), start.AddTicks(2) };

		for (var index = 0; index < instants.Length; index++)
		{
			var id = Guid.CreateVersion7();
			var timestamp = instants[index].ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);

			await context.Database.ExecuteSqlInterpolatedAsync($"""
				INSERT INTO Games (Id, Name, Slug, Image, MapId, CreatedAt, UpdatedAt, Status)
				VALUES ({id}, {"Game"}, {"game-" + index}, {"image"}, NULL, {timestamp}, {timestamp}, {"Draft"})
				""");
		}

		await context.Database.MigrateAsync();
		var query = (field, descending) switch
		{
			("createdAt", false) => context.Games.OrderBy(game => game.CreatedAt),
			("createdAt", true) => context.Games.OrderByDescending(game => game.CreatedAt),
			("updatedAt", false) => context.Games.OrderBy(game => game.UpdatedAt),
			_ => context.Games.OrderByDescending(game => game.UpdatedAt)
		};
		var pageQuery = query.ThenBy(game => game.Id).Skip(1).Take(2);
		var sql = pageQuery.ToQueryString();
		var games = await pageQuery.ToListAsync();
		var expected = descending ? instants.OrderDescending() : instants.Order();

		Assert.Contains("ORDER BY", sql);
		Assert.Contains("LIMIT", sql);
		Assert.Equal(expected.Skip(1).Take(2), games.Select(game => game.CreatedAt));
		Assert.All(games, game => Assert.Equal(game.CreatedAt, game.UpdatedAt));
	}
}
