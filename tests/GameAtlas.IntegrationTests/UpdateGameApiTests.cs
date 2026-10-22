using System.Net;
using System.Text;
using System.Text.Json;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class UpdateGameApiTests
{
	[Theory]
	[InlineData("application/json")]
	[InlineData("application/merge-patch+json")]
	public async Task UpdatesAllFieldsPreservesOmissionsAndClearsMap(string mediaType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		var mapId = Guid.NewGuid();
		var input = JsonSerializer.Serialize(new
		{
			name = " Updated ",
			slug = " updated ",
			image = " new-cover ",
			mapId,
			status = "published"
		});
		using var response = await PatchAsync(client, game.Id, input, mediaType);
		using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		var value = body.RootElement;

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(8, value.EnumerateObject().Count());
		Assert.Equal(game.Id, value.GetProperty("id").GetGuid());
		Assert.Equal(game.CreatedAt, value.GetProperty("createdAt").GetDateTimeOffset());
		Assert.Equal("Updated", value.GetProperty("name").GetString());
		Assert.Equal("updated", value.GetProperty("slug").GetString());
		Assert.Equal("new-cover", value.GetProperty("image").GetString());
		Assert.Equal(mapId, value.GetProperty("mapId").GetGuid());
		Assert.Equal("published", value.GetProperty("status").GetString());
		var updatedAt = value.GetProperty("updatedAt").GetDateTimeOffset();

		Assert.Equal(TimeSpan.Zero, updatedAt.Offset);
		Assert.InRange(updatedAt, game.UpdatedAt, DateTimeOffset.UtcNow);
		var stored = await LoadAsync(factory, game.Id);

		Assert.Equal(updatedAt, stored.UpdatedAt);
		Assert.Equal("Updated", stored.Name);
		Assert.Equal("updated", stored.Slug);
		Assert.Equal("new-cover", stored.Image);
		Assert.Equal(mapId, stored.MapId);
		Assert.Equal(GameStatus.Published, stored.Status);
		using var nameOnly = await PatchAsync(client, game.Id, "{\"name\":\"Renamed\"}", mediaType);
		var renamed = await LoadAsync(factory, game.Id);

		Assert.Equal(HttpStatusCode.OK, nameOnly.StatusCode);
		Assert.Equal("Renamed", renamed.Name);
		Assert.Equal(stored.Slug, renamed.Slug);
		Assert.Equal(stored.Image, renamed.Image);
		Assert.Equal(stored.MapId, renamed.MapId);
		Assert.Equal(stored.Status, renamed.Status);
		using var clearAndDraft = await PatchAsync(client, game.Id, "{\"mapId\":null,\"status\":\"draft\"}", mediaType);
		var draft = await LoadAsync(factory, game.Id);

		Assert.Equal(HttpStatusCode.OK, clearAndDraft.StatusCode);
		Assert.Null(draft.MapId);
		Assert.Equal(GameStatus.Draft, draft.Status);
		Assert.Equal(game.Id, draft.Id);
		Assert.Equal(game.CreatedAt, draft.CreatedAt);
	}

	[Theory]
	[InlineData("name", "Renamed")]
	[InlineData("slug", "renamed")]
	[InlineData("image", "plain-image")]
	[InlineData("mapId", "019a0000-0000-7000-8000-000000000001")]
	[InlineData("status", "published")]
	public async Task CanUpdateEachFieldIndependently(string field, string value)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, game.Id,
			JsonSerializer.Serialize(new Dictionary<string, string> { [field] = value }), "application/json");
		var stored = await LoadAsync(factory, game.Id);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(field == "name" ? value : game.Name, stored.Name);
		Assert.Equal(field == "slug" ? value : game.Slug, stored.Slug);
		Assert.Equal(field == "image" ? value : game.Image, stored.Image);
		Assert.Equal(field == "mapId" ? Guid.Parse(value) : game.MapId, stored.MapId);
		Assert.Equal(field == "status" ? GameStatus.Published : game.Status, stored.Status);
	}

	public static IEnumerable<object[]> InvalidBodies()
	{
		var inputs = new (string Body, int Status)[]
		{
			("{}", 422), ("{\"name\":null}", 422), ("{\"slug\":null}", 422),
			("{\"image\":null}", 422), ("{\"status\":null}", 422),
			("{\"name\":\"New\",\"image\":\" \"}", 422),
			("{\"slug\":\"bad--slug\"}", 422), ("{\"slug\":\"Upper\"}", 422),
			(JsonSerializer.Serialize(new { name = new string('n', 201) }), 422),
			(JsonSerializer.Serialize(new { slug = new string('s', 201) }), 422),
			(JsonSerializer.Serialize(new { image = new string('i', 2049) }), 422),
			("{", 400), ("[]", 400), ("null", 400), ("5", 400), ("\"text\"", 400),
			("{\"name\":5}", 400), ("{\"slug\":false}", 400), ("{\"image\":{}}", 400),
			("{\"mapId\":\"bad\"}", 400), ("{\"status\":\"unknown\"}", 400),
			("{\"status\":1}", 400), ("{\"name\":\"New\",\"unknown\":true}", 400),
			("{\"name\":\"New\",\"id\":null}", 400),
			("{\"name\":\"New\",\"createdAt\":null}", 400),
			("{\"name\":\"New\",\"updatedAt\":null}", 400)
		};

		foreach (var mediaType in new[] { "application/json", "application/merge-patch+json" })
		{
			foreach (var (body, status) in inputs)
				yield return [body, status, mediaType];
		}
	}

	[Theory]
	[MemberData(nameof(InvalidBodies))]
	public async Task RejectsInvalidRequestsWithoutChangingStoredGame(string body, int status, string mediaType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, game.Id, body, mediaType);

		await AssertProblemAsync(response, (HttpStatusCode)status);
		AssertSameGame(game, await LoadAsync(factory, game.Id));
	}

	[Theory]
	[InlineData("application/json")]
	[InlineData("application/merge-patch+json")]
	public async Task AcceptsCharsetAndNormalizedNoOpsWithoutSaving(string mediaType)
	{
		var observer = new UpdateGameHandlerTests.UpdateSaveObserver();
		await using var factory = new GameApiFactory(interceptor: observer);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, game.Id,
			"{\"name\":\" Game \",\"slug\":\" game \",\"image\":\" cover \",\"mapId\":null,\"status\":\"draft\"}", mediaType);
		using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(game.UpdatedAt, json.RootElement.GetProperty("updatedAt").GetDateTimeOffset());
		Assert.Equal(0, observer.UpdateSaves);
		AssertSameGame(game, await LoadAsync(factory, game.Id));
	}

	[Theory]
	[InlineData("text/plain", "{\"name\":\"New\"}")]
	[InlineData("text/plain", "{")]
	[InlineData("application/json-patch+json", "[]")]
	[InlineData("application/json-patch+json", "{")]
	[InlineData("application/custom+json", "{")]
	public async Task RejectsUnsupportedMediaTypeBeforeDeserialization(string mediaType, string body)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, game.Id, body, mediaType);

		await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
		AssertSameGame(game, await LoadAsync(factory, game.Id));
	}

	[Fact]
	public async Task MissingGameInvalidRouteAndPutDoNotUpdateTheCatalog()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var missing = await PatchAsync(client, Guid.NewGuid(), "{\"name\":\"New\"}", "application/json");
		using var invalid = await client.PatchAsync("/games/not-a-guid",
			new StringContent("{\"name\":\"New\"}", Encoding.UTF8, "application/json"));
		using var put = await client.PutAsync($"/games/{Guid.NewGuid()}",
			new StringContent("{\"name\":\"New\"}", Encoding.UTF8, "application/json"));

		await AssertProblemAsync(missing, HttpStatusCode.NotFound);
		await AssertProblemAsync(invalid, HttpStatusCode.BadRequest);
		Assert.Equal(HttpStatusCode.MethodNotAllowed, put.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();

		Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.ToListAsync());
	}

	[Fact]
	public async Task ExistingSlugConflictDoesNotPersistAnyPartOfThePatch()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var other = await UpdateGameHandlerTests.SeedAsync(factory, "taken");
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, game.Id,
			"{\"name\":\"New\",\"slug\":\" taken \"}", "application/json");

		await AssertProblemAsync(response, HttpStatusCode.Conflict);
		AssertSameGame(game, await LoadAsync(factory, game.Id));
		AssertSameGame(other, await LoadAsync(factory, other.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PersistenceFailuresReturnConflictOnlyForRecognizedSlugViolations(bool slugRace)
	{
		var interceptor = new UpdateFailureInterceptor(slugRace);
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var rival = await UpdateGameHandlerTests.SeedAsync(factory, "rival");

		interceptor.RivalId = rival.Id;
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, game.Id,
			"{\"name\":\"New\",\"slug\":\"raced\"}", "application/merge-patch+json");

		await AssertProblemAsync(response, slugRace ? HttpStatusCode.Conflict : HttpStatusCode.InternalServerError);
		AssertSameGame(game, await LoadAsync(factory, game.Id));
	}

	private static async Task<HttpResponseMessage> PatchAsync(HttpClient client, Guid id, string body, string mediaType)
	{
		using var content = new StringContent(body, Encoding.UTF8, mediaType);

		return await client.PatchAsync($"/games/{id}", content);
	}

	private static async Task<Game> LoadAsync(GameApiFactory factory, Guid id)
	{
		await using var scope = factory.Services.CreateAsyncScope();

		return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.SingleAsync(game => game.Id == id);
	}

	private static void AssertSameGame(Game expected, Game actual)
	{
		Assert.Equal((expected.Id, expected.Name, expected.Slug, expected.Image, expected.MapId,
			expected.Status, expected.CreatedAt, expected.UpdatedAt),
			(actual.Id, actual.Name, actual.Slug, actual.Image, actual.MapId,
				actual.Status, actual.CreatedAt, actual.UpdatedAt));
	}

	private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
	{
		var text = await response.Content.ReadAsStringAsync();

		Assert.Equal(status, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		using var document = JsonDocument.Parse(text);

		Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
		Assert.DoesNotContain("SQLite", text);
		Assert.DoesNotContain("private storage", text);
		Assert.DoesNotContain("UPDATE Games", text);
		Assert.DoesNotContain("Data Source", text);
	}

	private sealed class UpdateFailureInterceptor(bool slugRace) : SaveChangesInterceptor
	{
		public Guid RivalId { get; set; }

		public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
			DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			var context = eventData.Context!;

			if (context.ChangeTracker.Entries<Game>().Any(entry => entry.State == EntityState.Modified))
			{
				if (!slugRace)
					throw new InvalidOperationException("private storage failure");

				// Simulate a competing writer claiming the slug after the handler's uniqueness check.
				await context.Database.ExecuteSqlInterpolatedAsync(
					$"UPDATE Games SET Slug = {"raced"} WHERE Id = {RivalId}", cancellationToken);
			}

			return result;
		}
	}
}
