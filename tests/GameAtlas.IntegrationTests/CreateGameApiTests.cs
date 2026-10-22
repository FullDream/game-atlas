using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Abstractions.Persistence;
using GameAtlas.Application.Games.CreateGame;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CreateGameApiTests
{
	[Theory]
	[InlineData("omitted")]
	[InlineData("null")]
	[InlineData("supplied")]
	public async Task CreatesNormalizedDraftAndReturnsPersistedMetadata(string mapMode)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var mapId = mapMode == "supplied" ? Guid.NewGuid() : (Guid?)null;
		var input = new Dictionary<string, object?>
		{
			["name"] = " Elden Ring ",
			["slug"] = " elden-ring-2 ",
			["image"] = " cover ",
			["status"] = "published",
			["id"] = Guid.Empty,
			["createdAt"] = "2000-01-01T00:00:00Z"
		};

		if (mapMode != "omitted")
			input["mapId"] = mapId;

		using var response = await client.PostAsJsonAsync("/games", input);
		using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		var body = json.RootElement;

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(8, body.EnumerateObject().Count());
		Assert.Equal(7, body.GetProperty("id").GetGuid().Version);
		Assert.Equal("Elden Ring", body.GetProperty("name").GetString());
		Assert.Equal("elden-ring-2", body.GetProperty("slug").GetString());
		Assert.Equal("cover", body.GetProperty("image").GetString());
		Assert.Equal("draft", body.GetProperty("status").GetString());
		var createdAt = body.GetProperty("createdAt").GetDateTimeOffset();

		Assert.Equal(createdAt, body.GetProperty("updatedAt").GetDateTimeOffset());
		Assert.Equal(TimeSpan.Zero, createdAt.Offset);
		Assert.InRange(createdAt, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow);
		await using var scope = factory.Services.CreateAsyncScope();
		var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.SingleAsync();

		Assert.Equal(body.GetProperty("id").GetGuid(), stored.Id);
		Assert.Equal(mapId, stored.MapId);
		Assert.Equal(createdAt, stored.CreatedAt);
		Assert.Equal("Elden Ring", stored.Name);
		Assert.Equal("elden-ring-2", stored.Slug);
		Assert.Equal("cover", stored.Image);
		Assert.Equal(GameStatus.Draft, stored.Status);

		if (mapId.HasValue)
			Assert.Equal(mapId.Value, body.GetProperty("mapId").GetGuid());
		else
			Assert.Equal(JsonValueKind.Null, body.GetProperty("mapId").ValueKind);
	}

	public static IEnumerable<object[]> InvalidInputs()
	{
		foreach (var field in new[] { "name", "slug", "image" })
		{
			foreach (var value in new string?[] { null, "", " " })
				yield return [field, value!];

			yield return [field, new string('a', field == "image" ? 2049 : 201)];
		}

		foreach (var slug in new[] { "Upper", "two words", "two--words", "-game", "game-", "игра" })
			yield return ["slug", slug];
	}

	[Theory]
	[MemberData(nameof(InvalidInputs))]
	public async Task InvalidMetadataReturnsValidationProblemWithoutSaving(string field, string? value)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var input = new Dictionary<string, object?> { ["name"] = "Game", ["slug"] = "game", ["image"] = "cover" };

		input[field] = value;
		using var response = await client.PostAsJsonAsync("/games", input);
		var problem = await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);

		Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
		await AssertCountAsync(factory, 0);
	}

	[Fact]
	public async Task MissingMetadataReturns422AndExactLimitsAreAccepted()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var invalid = await client.PostAsJsonAsync("/games", new { });

		await AssertProblemAsync(invalid, HttpStatusCode.UnprocessableEntity);
		using var valid = await client.PostAsJsonAsync("/games", new
		{
			name = new string('n', 200),
			slug = new string('s', 200),
			image = new string('i', 2048)
		});

		Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
		await AssertCountAsync(factory, 1);
	}

	[Theory]
	[InlineData("{")]
	[InlineData("{\"name\":\"Game\",\"slug\":\"game\",\"image\":\"cover\",\"mapId\":\"invalid\"}")]
	public async Task MalformedRequestReturns400(string input)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsync("/games", new StringContent(input, Encoding.UTF8, "application/json"));

		await AssertProblemAsync(response, HttpStatusCode.BadRequest);
		await AssertCountAsync(factory, 0);
	}

	[Fact]
	public async Task SequentialAndConcurrentDuplicatesReturnConflict()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var input = new { name = "Game", slug = "game", image = "cover" };
		using var success = await client.PostAsJsonAsync("/games", input);
		using var conflict = await client.PostAsJsonAsync("/games", input);

		Assert.Equal(HttpStatusCode.OK, success.StatusCode);
		await AssertProblemAsync(conflict, HttpStatusCode.Conflict);
		var concurrent = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
			client.PostAsJsonAsync("/games", new { name = "Game", slug = "raced-game", image = "cover" })));

		try
		{
			Assert.Single(concurrent, response => response.StatusCode == HttpStatusCode.OK);
			await AssertProblemAsync(Assert.Single(concurrent, response => response.StatusCode != HttpStatusCode.OK),
				HttpStatusCode.Conflict);
			await AssertCountAsync(factory, 2);
		}
		finally
		{
			foreach (var response in concurrent)
				response.Dispose();
		}
	}

	[Fact]
	public async Task StartupDoesNotApplyMigrationsAndStorageFailureReturnsSafe500()
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateHttpsClient();

		Assert.False(File.Exists(factory.DatabasePath));
		using var response = await client.PostAsJsonAsync("/games", new { name = "Game", slug = "game", image = "cover" });

		await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
	}

	[Fact]
	public async Task MediatorCreatesAndRejectsDuplicatesThroughApplicationPipeline()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await using var scope = factory.Services.CreateAsyncScope();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var invalid = await mediator.Send(new CreateGame(null, null, null));
		var valid = await mediator.Send(new CreateGame(" Game ", " game ", " cover "));
		var duplicate = await mediator.Send(new CreateGame("Other", "game", "cover"));

		Assert.True(invalid.IsFailure);
		Assert.True(valid.IsSuccess);
		Assert.Equal("Game", valid.Value.Name);
		Assert.True(duplicate.IsFailure);
		Assert.Contains(duplicate.Errors, error => error.Type == GameAtlas.Domain.Result.ErrorType.Conflict);
		await AssertCountAsync(factory, 1);
	}

	[Fact]
	public async Task RealUniqueViolationIsClassifiedAndTranslatedDirectlyTo409()
	{
		await using var source = new GameApiFactory();

		await source.MigrateAsync();
		await using var scope = source.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		context.Games.Add(Game.Create("Game", "game", "cover").Value!);
		await context.SaveChangesAsync();
		context.Games.Add(Game.Create("Other", "game", "cover").Value!);
		var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
		var classifier = new SqlitePersistenceFailureClassifier();

		Assert.True(classifier.IsGameSlugConflict(exception));
		await using var failing = new GameApiFactory(interceptor: new ThrowOnSave(exception));

		await failing.MigrateAsync();
		using var client = failing.CreateHttpsClient();
		using var response = await client.PostAsJsonAsync("/games", new { name = "New", slug = "new", image = "cover" });

		await AssertProblemAsync(response, HttpStatusCode.Conflict);
		await AssertCountAsync(failing, 0);
		context.ChangeTracker.Clear();
		var unrelated = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
			context.Database.ExecuteSqlRawAsync("INSERT INTO Games (Id) VALUES ('bad')"));

		Assert.False(classifier.IsGameSlugConflict(new DbUpdateException("unrelated", unrelated)));
		Assert.False(classifier.IsGameSlugConflict(new InvalidOperationException("unexpected")));
	}

	private sealed class ThrowOnSave(Exception exception) : SaveChangesInterceptor
	{
		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
			DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
			throw exception;
	}

	private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
	{
		var text = await response.Content.ReadAsStringAsync();

		Assert.Equal(expected, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		using var document = JsonDocument.Parse(text);

		Assert.Equal((int)expected, document.RootElement.GetProperty("status").GetInt32());
		Assert.DoesNotContain("SQLite", text);
		Assert.DoesNotContain("SELECT", text);
		Assert.DoesNotContain("Data Source", text);

		return document.RootElement.Clone();
	}

	private static async Task AssertCountAsync(GameApiFactory factory, int count)
	{
		await using var scope = factory.Services.CreateAsyncScope();

		Assert.Equal(count, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.CountAsync());
	}
}
