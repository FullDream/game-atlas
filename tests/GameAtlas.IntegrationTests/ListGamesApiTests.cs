using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Text.Json;
using GameAtlas.Application.Common.Sorting;
using GameAtlas.Application.Games;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class ListGamesApiTests
{
	[Fact]
	public async Task DefaultsReturnCompleteMetadataAndAllStatusesWithoutOrderingOrWrites()
	{
		var capture = new CommandCapture();
		await using var factory = new GameApiFactory(interceptor: capture);
		var games = await SeedAsync(factory, Enumerable.Range(0, 25).Select(index => $"Game {index:D2}").ToArray());

		capture.Commands.Clear();
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync("/games");
		var page = await ReadPageAsync(response);

		Assert.Equal(0, page.GetProperty("offset").GetInt32());
		Assert.Equal(20, page.GetProperty("limit").GetInt32());
		Assert.Equal(25, page.GetProperty("total").GetInt32());
		Assert.Equal(4, page.EnumerateObject().Count());
		var items = page.GetProperty("items").EnumerateArray().ToArray();

		Assert.Equal(20, items.Length);
		Assert.Contains(items, item => item.GetProperty("status").GetString() == "draft");
		Assert.Contains(items, item => item.GetProperty("status").GetString() == "published");

		foreach (var item in items)
		{
			var game = games.Single(game => game.Id == item.GetProperty("id").GetGuid());

			Assert.Equal(8, item.EnumerateObject().Count());
			Assert.Equal(game.Name, item.GetProperty("name").GetString());
			Assert.Equal(game.Slug, item.GetProperty("slug").GetString());
			Assert.Equal(game.Image, item.GetProperty("image").GetString());
			Assert.Equal(game.MapId, item.GetProperty("mapId").GetGuid());
			Assert.Equal(game.CreatedAt, item.GetProperty("createdAt").GetDateTimeOffset());
			Assert.Equal(game.UpdatedAt, item.GetProperty("updatedAt").GetDateTimeOffset());
		}

		var commands = capture.Commands.ToArray();

		Assert.Equal(2, commands.Length);
		Assert.Contains("COUNT(*)", commands[0]);
		Assert.Contains("LIMIT", commands[1]);
		Assert.Contains("OFFSET", commands[1]);
		Assert.All(commands, command => Assert.DoesNotContain("ORDER BY", command));
		Assert.All(commands, command => Assert.StartsWith("SELECT", command));
	}

	[Theory]
	[InlineData("name", "asc")]
	[InlineData("createdAt", "desc")]
	[InlineData("updatedAt", "asc")]
	[InlineData("NAME", "DESC")]
	[InlineData("Name", "Asc")]
	[InlineData("Name", "Desc")]
	[InlineData("CreatedAt", "Asc")]
	[InlineData("CreatedAt", "Desc")]
	[InlineData("UpdatedAt", "Asc")]
	[InlineData("UpdatedAt", "Desc")]
	[InlineData("Name", null)]
	[InlineData("0", "0")]
	[InlineData("1", "1")]
	[InlineData("Name,CreatedAt", "Asc")]
	[InlineData(" Name ", "Desc")]
	public async Task SortsBeforePaginationAndKeepsTotal(string field, string? direction)
	{
		var capture = new CommandCapture();
		await using var factory = new GameApiFactory(interceptor: capture);
		var games = await SeedAsync(factory, ["Zulu", "Same", "Alpha", "Same", "Beta"]);

		capture.Commands.Clear();
		using var client = factory.CreateHttpsClient();
		var url = $"/games?offset=1&limit=2&sortBy={field}";

		if (direction is not null)
			url += $"&sortDirection={direction}";

		using var response = await client.GetAsync(url);
		var page = await ReadPageAsync(response);
		var descending = direction is not null && Enum.Parse<SortDirection>(direction, ignoreCase: true) == SortDirection.Desc;
		IOrderedEnumerable<Game> expected = Enum.Parse<GameSortBy>(field, ignoreCase: true) switch
		{
			GameSortBy.Name => descending ? games.OrderByDescending(game => game.Name, StringComparer.Ordinal)
				: games.OrderBy(game => game.Name, StringComparer.Ordinal),
			GameSortBy.CreatedAt => descending ? games.OrderByDescending(game => game.CreatedAt)
				: games.OrderBy(game => game.CreatedAt),
			_ => descending ? games.OrderByDescending(game => game.UpdatedAt) : games.OrderBy(game => game.UpdatedAt)
		};

		Assert.Equal(expected.ThenBy(game => game.Id).Skip(1).Take(2).Select(game => game.Id),
			page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
		Assert.Equal(5, page.GetProperty("total").GetInt32());
		var sql = capture.Commands.Last();

		Assert.True(sql.IndexOf("ORDER BY", StringComparison.Ordinal) < sql.IndexOf("LIMIT", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(" RING ", 2)]
	[InlineData("", 8)]
	[InlineData("   ", 8)]
	[InlineData(null, 8)]
	[InlineData("missing", 0)]
	[InlineData("%", 1)]
	[InlineData("_", 1)]
	[InlineData("\\", 1)]
	[InlineData("Ведьмак", 1)]
	public async Task SearchUsesLiteralTrimmedSubstringAndFilteredTotal(string? search, int total)
	{
		await using var factory = new GameApiFactory();

		await SeedAsync(factory, ["Elden Ring", "Other ring", "Percent %", "Under_score", "Back\\slash",
			"Ведьмак", "Alpha", "Beta"]);
		using var client = factory.CreateHttpsClient();
		var url = "/games?limit=1&sortBy=name" + (search is null ? "" : "&search=" + Uri.EscapeDataString(search));
		using var response = await client.GetAsync(url);
		var page = await ReadPageAsync(response);

		Assert.Equal(total, page.GetProperty("total").GetInt32());
		Assert.Equal(Math.Min(total, 1), page.GetProperty("items").GetArrayLength());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task EmptyAndOutOfRangePagesReturn200(bool seed)
	{
		await using var factory = new GameApiFactory();

		await SeedAsync(factory, seed ? ["Game"] : []);
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync("/games?offset=10&limit=100");
		var page = await ReadPageAsync(response);

		Assert.Equal(0, page.GetProperty("items").GetArrayLength());
		Assert.Equal(seed ? 1 : 0, page.GetProperty("total").GetInt32());
		Assert.Equal(10, page.GetProperty("offset").GetInt32());
		Assert.Equal(100, page.GetProperty("limit").GetInt32());
	}

	[Theory]
	[InlineData("offset=-1", 422, "offset")]
	[InlineData("limit=0", 422, "limit")]
	[InlineData("limit=-1", 422, "limit")]
	[InlineData("limit=101", 422, "limit")]
	[InlineData("sortBy=slug", 400, null)]
	[InlineData("sortBy=99", 422, "sortBy")]
	[InlineData("sortBy=-1", 422, "sortBy")]
	[InlineData("sortBy=", 400, null)]
	[InlineData("sortBy=%20", 400, null)]
	[InlineData("sortBy=Name&sortDirection=sideways", 400, null)]
	[InlineData("sortBy=Name&sortDirection=99", 422, "sortDirection")]
	[InlineData("sortBy=Name&sortDirection=-1", 422, "sortDirection")]
	[InlineData("sortBy=Name&sortDirection=", 400, null)]
	[InlineData("sortBy=Name&sortDirection=%20", 400, null)]
	[InlineData("sortDirection=Asc", 422, "sortBy")]
	[InlineData("offset=bad", 400, null)]
	[InlineData("limit=2147483648", 400, null)]
	[InlineData("limit=1.5", 400, null)]
	public async Task RejectsInvalidQueryWithProblemDetails(string query, int status, string? field)
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync("/games?" + query);

		Assert.Equal(status, (int)response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		Assert.Equal(status, document.RootElement.GetProperty("status").GetInt32());

		if (field is not null)
			Assert.True(document.RootElement.GetProperty("errors").TryGetProperty(field, out _));
	}

	[Fact]
	public async Task UnexpectedStorageFailureReturnsSafe500()
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync("/games");
		var content = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		Assert.DoesNotContain("SQLite", content);
		Assert.DoesNotContain("SELECT", content);
	}

	private static async Task<Game[]> SeedAsync(GameApiFactory factory, string[] names)
	{
		await factory.MigrateAsync();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var games = names.Select((name, index) => Game.Create(name, "game-" + index, "image", Guid.NewGuid()).Value!)
			.ToArray();

		for (var index = 0; index < games.Length; index++)
		{
			var entry = context.Games.Add(games[index]);

			entry.Property(game => game.CreatedAt).CurrentValue = start.AddTicks(index);
			entry.Property(game => game.UpdatedAt).CurrentValue = start.AddTicks(games.Length - index);
			entry.Property(game => game.Status).CurrentValue = index % 2 == 0 ? GameStatus.Draft : GameStatus.Published;
		}

		await context.SaveChangesAsync();

		return games;
	}

	private static async Task<JsonElement> ReadPageAsync(HttpResponseMessage response)
	{
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		return document.RootElement.Clone();
	}

	private sealed class CommandCapture : DbCommandInterceptor
	{
		internal ConcurrentQueue<string> Commands { get; } = new();

		public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
			DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
			CancellationToken cancellationToken = default)
		{
			Commands.Enqueue(command.CommandText);

			return ValueTask.FromResult(result);
		}
	}
}
