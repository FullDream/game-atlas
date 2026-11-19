using System.Net;
using System.Net.Http.Json;
using System.Text;
using GameAtlas.Application.Games;
using GameAtlas.Application.Maps;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.ValueObjects;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MapSlugTests
{
	[Theory]
	[InlineData("{}")]
	[InlineData("{\"name\":\"World\"}")]
	[InlineData("{\"slug\":\"world\"}")]
	[InlineData("{\"name\":null,\"slug\":\"world\"}")]
	[InlineData("{\"name\":\"World\",\"slug\":null}")]
	[InlineData("{\"name\":\"World\",\"slug\":\"Upper\"}")]
	public async Task CreationRequiresValidNameAndSlug(string json)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = await factory.CreateHttpsClientAsync();
		using var response = await client.PostAsync(MapTestData.CollectionFor(game.Id), new StringContent(json, Encoding.UTF8, "application/json"));

		Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();

		Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Maps.ToArrayAsync());
	}

	[Theory]
	[InlineData("application/json")]
	[InlineData("application/merge-patch+json; charset=utf-8")]
	public async Task PatchPreservesOmissionsNoOpsAndAtomicFailures(string mediaType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = await factory.CreateHttpsClientAsync();
		using var creation = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = " World ", slug = " world " });
		var map = await MapTestData.ReadAsync(creation);
		var path = MapTestData.PathFor(game.Id, map.Id);

		Assert.Equal("world", map.Slug);
		using var noOp = await client.PatchAsync(path, new StringContent("{\"slug\":\" world \"}", Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType)));

		Assert.Equal(map.UpdatedAt, (await MapTestData.ReadAsync(noOp)).UpdatedAt);

		foreach (var invalid in new[] { "null", "\"\"", "\" \"", "\"bad--slug\"", "\"Upper\"", "\"игра\"", "\"" + new string('s', 201) + "\"" })
		{
			using var rejected = await client.PatchAsync(path, new StringContent("{\"name\":\"Changed\",\"slug\":" + invalid + "}", Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType)));

			Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
		}

		using var read = await client.GetAsync(path);
		var unchanged = await MapTestData.ReadAsync(read);

		Assert.Equal((map.Id, map.Name, map.Slug, map.CreatedAt, map.UpdatedAt),
			(unchanged.Id, unchanged.Name, unchanged.Slug, unchanged.CreatedAt, unchanged.UpdatedAt));

		using var renamed = await client.PatchAsJsonAsync(path, new { name = "Renamed" });

		Assert.Equal("world", (await MapTestData.ReadAsync(renamed)).Slug);
		using var updated = await client.PatchAsync(path, new StringContent("{\"slug\":\" new-world \"}", Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType)));
		var changed = await MapTestData.ReadAsync(updated);

		Assert.Equal("new-world", changed.Slug);
		Assert.Equal(map.Id, changed.Id);
		Assert.Equal("Renamed", changed.Name);
		var returnedGame = (await client.GetFromJsonAsync<GameDto>($"/games/{game.Id}", MapTestData.JsonOptions))!;

		Assert.Equal("new-world", Assert.Single(returnedGame.Maps).Slug);
		Assert.Equal(game.UpdatedAt, returnedGame.UpdatedAt);
		var maps = (await client.GetFromJsonAsync<MapDto[]>(MapTestData.CollectionFor(game.Id), MapTestData.JsonOptions))!;

		Assert.Equal("new-world", Assert.Single(maps).Slug);
	}

	[Fact]
	public async Task UniqueSlugIsScopedToGameAndReusedAfterDeletion()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		var other = await UpdateGameHandlerTests.SeedAsync(factory, "other");
		using var client = await factory.CreateHttpsClientAsync();
		using var first = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "World", slug = "world" });
		var map = await MapTestData.ReadAsync(first);
		using var duplicate = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "Other", slug = "world" });
		using var crossGame = await client.PostAsJsonAsync(MapTestData.CollectionFor(other.Id), new { name = "World", slug = "world" });
		using var second = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "Dungeon", slug = "dungeon" });
		var dungeon = await MapTestData.ReadAsync(second);
		using var rejectedPatch = await client.PatchAsJsonAsync(MapTestData.PathFor(game.Id, dungeon.Id), new { name = "Changed", slug = "world" });

		Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, rejectedPatch.StatusCode);
		Assert.Equal(HttpStatusCode.OK, crossGame.StatusCode);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id, dungeon.Id));

		Assert.Equal("Dungeon", (await MapTestData.ReadAsync(read)).Name);
		using var deleted = await client.DeleteAsync(MapTestData.PathFor(game.Id, map.Id));
		using var reused = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "Reused", slug = "world" });

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Equal(HttpStatusCode.OK, reused.StatusCode);
	}

	[Fact]
	public async Task CompetingCreatesLeaveExactlyOneOwner()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = await factory.CreateHttpsClientAsync();

		var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(index =>
			client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = $"Map {index}", slug = "shared" })));

		try
		{
			Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
			Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
			var maps = (await client.GetFromJsonAsync<MapDto[]>(MapTestData.CollectionFor(game.Id), MapTestData.JsonOptions))!;

			Assert.Equal("shared", Assert.Single(maps).Slug);
		}
		finally
		{
			foreach (var response in responses)
				response.Dispose();
		}
	}

	[Fact]
	public async Task ConverterQueriesAndDatabaseConstraintUseSlugValue()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var slug = Slug.Create("world").Value!;

		context.Maps.Add(Map.Create(game.Id, "World", slug).Value!);
		await context.SaveChangesAsync();
		context.ChangeTracker.Clear();

		Assert.Equal(slug, (await context.Maps.SingleAsync(map => map.GameId == game.Id && map.Slug == slug)).Slug);
		context.Maps.Add(Map.Create(game.Id, "Other", Slug.Create("world").Value!).Value!);
		var failure = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		Assert.True(new SqlitePersistenceFailureClassifier().IsMapSlugConflict(failure));
		Assert.False(new SqlitePersistenceFailureClassifier().IsMapNameConflict(failure));
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, false)]
	[InlineData(true, true)]
	[InlineData(false, true)]
	public async Task RecognizedSaveTimeConflictReturns409AndUnrelatedFailureReturns500(bool slugConflict, bool update)
	{
		var interceptor = new SaveFailure(slugConflict);
		await using var factory = new GameApiFactory(interceptor: interceptor);

		await factory.MigrateAsync();
		var game = await UpdateGameHandlerTests.SeedAsync(factory, "game");
		using var client = await factory.CreateHttpsClientAsync();
		using var creation = await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "Existing", slug = "existing" });
		var map = await MapTestData.ReadAsync(creation);

		interceptor.Enabled = true;

		using var response = update
			? await client.PatchAsJsonAsync(MapTestData.PathFor(game.Id, map.Id), new { name = "Changed", slug = "world" })
			: await client.PostAsJsonAsync(MapTestData.CollectionFor(game.Id), new { name = "World", slug = "world" });

		Assert.Equal(slugConflict ? HttpStatusCode.Conflict : HttpStatusCode.InternalServerError, response.StatusCode);
		using var read = await client.GetAsync(MapTestData.PathFor(game.Id, map.Id));
		var unchanged = await MapTestData.ReadAsync(read);

		Assert.Equal((map.Name, map.Slug, map.UpdatedAt), (unchanged.Name, unchanged.Slug, unchanged.UpdatedAt));
	}

	private sealed class SaveFailure(bool slugConflict) : SaveChangesInterceptor
	{
		public bool Enabled { get; set; }

		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
			InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			if (Enabled)
				throw slugConflict
					? new DbUpdateException("Conflict", new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: Maps.GameId, Maps.Slug'.", 19, 2067))
					: new InvalidOperationException("Unrelated failure");

			return base.SavingChangesAsync(eventData, result, cancellationToken);
		}
	}
}
