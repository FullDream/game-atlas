using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Games;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class GameImageTests
{
	[Fact]
	public async Task SharedCoverLifecycleReturnsCompleteCurrentImagesAndPreservesAssets()
	{
		var capture = new SqlReadCapture();

		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client);
		var next = await MediaTestData.UploadImageAsync(client, "svg");
		using var created = await client.PostAsJsonAsync("/games", new { name = "Game", slug = "game", coverId = image.Id });

		created.EnsureSuccessStatusCode();
		var game = (await created.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!;

		Assert.Equal(image, game.Cover);

		using var payload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());

		Assert.Equal(1, payload.RootElement
			.GetProperty("cover").EnumerateObject().Count(property => property.Name == "type"));
		using var second = await client.PostAsJsonAsync("/games", new { name = "Other", slug = "other", coverId = image.Id });

		second.EnsureSuccessStatusCode();
		var other = (await second.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!;
		using var blocked = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
		using var same = await client.PatchAsJsonAsync($"/games/{game.Id}", new { coverId = image.Id });

		Assert.Equal(game.UpdatedAt, (await same.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!.UpdatedAt);

		using var metadata = await client.PatchAsJsonAsync($"/media/assets/{image.Id}", new { alt = "New alt", name = "Renamed" });

		metadata.EnsureSuccessStatusCode();

		capture.Enabled = true;
		var page = await client.GetFromJsonAsync<GamePage>("/games?limit=1&sortBy=name", MapTestData.JsonOptions);

		capture.Enabled = false;
		var listed = Assert.Single(page!.Items);

		Assert.Equal(2, page.Total);
		Assert.Equal("New alt", listed.Cover.Alt);
		Assert.Equal(image.Placeholder, listed.Cover.Placeholder);
		Assert.Equal(game.UpdatedAt, listed.UpdatedAt);
		Assert.InRange(capture.Commands.Count, 2, 6);
		Assert.Contains(capture.Commands, command => command.Contains("JOIN"));
		using var replace = await client.PatchAsJsonAsync($"/games/{game.Id}", new { coverId = next.Id });

		Assert.Equal(next, (await replace.Content.ReadFromJsonAsync<GameDto>(MapTestData.JsonOptions))!.Cover);
		capture.Commands.Clear();

		capture.Enabled = true;
		var distinctCovers = await client.GetFromJsonAsync<GamePage>("/games", MapTestData.JsonOptions);

		capture.Enabled = false;

		Assert.Equal(2, distinctCovers!.Items.Select(item => item.Cover.Id).Distinct().Count());
		Assert.Equal(6, capture.Commands.Count);

		using var stillUsed = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.Conflict, stillUsed.StatusCode);

		using var deleteOther = await client.DeleteAsync($"/games/{other.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleteOther.StatusCode);

		using var deleteOld = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleteOld.StatusCode);

		using var deleteGame = await client.DeleteAsync($"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleteGame.StatusCode);
		using var scope = factory.Services.CreateScope();

		Assert.True(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().MediaAssets.AnyAsync(asset => asset.Id == next.Id));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AssignmentAndDeletionCannotLeaveDanglingCovers(bool update)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "existing");
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client, "svg");
		var assignment = update
			? client.PatchAsJsonAsync($"/games/{game.Id}", new { coverId = image.Id })
			: client.PostAsJsonAsync("/games", new { name = "New", slug = "new", coverId = image.Id });
		var responses = await Task.WhenAll(assignment, client.DeleteAsync($"/media/assets/{image.Id}"));
		using var assigned = responses[0];
		using var deleted = responses[1];

		Assert.True(assigned.StatusCode == HttpStatusCode.OK && deleted.StatusCode == HttpStatusCode.Conflict ||
			assigned.StatusCode == HttpStatusCode.NotFound && deleted.StatusCode == HttpStatusCode.NoContent);

		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Equal(0, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").SingleAsync());
	}

	[Fact]
	public async Task MissingOriginalStillReturnsMetadataAndBlocksDeletion()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		var page = await client.GetFromJsonAsync<GamePage>("/games", MapTestData.JsonOptions);

		Assert.Equal(game.CoverId, Assert.Single(page!.Items).Cover.Id);
		using var missing = await client.GetAsync($"/media/files/{game.CoverId}");
		using var blocked = await client.DeleteAsync($"/media/assets/{game.CoverId}");

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
	}

	[Theory]
	[InlineData("missing", 404)]
	[InlineData("video", 422)]
	[InlineData("folder", 422)]
	[InlineData("empty", 422)]
	[InlineData("null", 422)]
	[InlineData("malformed", 400)]
	public async Task InvalidImagesRejectCreateAndPatchAtomically(string kind, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "existing");
		using var client = factory.CreateHttpsClient();
		var folder = await MediaFolderTests.CreateAsync(client, "Folder");
		object? id = kind switch
		{
			"video" => MediaTestData.VideoId,
			"folder" => folder.Id,
			"empty" => Guid.Empty,
			"null" => null,
			"malformed" => "bad",
			_ => Guid.NewGuid()
		};
		using var created = await client.PostAsJsonAsync("/games", new { name = "New", slug = "new", coverId = id });
		using var patched = await client.PatchAsJsonAsync($"/games/{game.Id}", new { name = "Changed", coverId = id });

		Assert.Equal((HttpStatusCode)status, created.StatusCode);
		Assert.Equal((HttpStatusCode)status, patched.StatusCode);

		await using var scope = factory.Services.CreateAsyncScope();
		var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.SingleAsync();
		DeleteGameHandlerTests.AssertSameGame(game, stored);
	}

	[Fact]
	public async Task MigrationGuardsLegacyRowsAndRollbackAndRestrictsAssetDeletion()
	{
		await using var factory = new GameApiFactory();

		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var previous = context.Database.GetMigrations().Last(name => string.CompareOrdinal(name, "20261028202100") < 0);
		var migrator = context.GetService<IMigrator>();

		await migrator.MigrateAsync(previous);
		var legacy = await MediaTestData.SeedLegacyGameAsync(context);
		var refusal = await Assert.ThrowsAsync<SqliteException>(() => migrator.MigrateAsync());

		Assert.Contains("GameImages_RequiresEmptyLegacyGames", refusal.Message);
		Assert.Equal("cover", await context.Database.SqlQuery<string>($"SELECT Image AS Value FROM Games").SingleAsync());

		await context.Database.ExecuteSqlAsync($"DELETE FROM Games WHERE Id = {legacy}");

		await migrator.MigrateAsync();

		Assert.False(context.Database.HasPendingModelChanges());

		await MediaTestData.EnsureCoverAsync(context);
		context.Games.Add(Game.Create("Game", "game", MediaTestData.CoverId).Value!);

		await context.SaveChangesAsync();

		await Assert.ThrowsAsync<SqliteException>(() => context.MediaAssets.Where(asset => asset.Id == MediaTestData.CoverId).ExecuteDeleteAsync());
		var rollback = await Assert.ThrowsAsync<SqliteException>(() => migrator.MigrateAsync(previous));

		Assert.Contains("GameImages_RestoreBackupForNonemptyGames", rollback.Message);
		await context.Database.MigrateAsync();
		Assert.Equal(MediaTestData.CoverId, (await context.Games.Include(game => game.Cover).SingleAsync()).Cover.Id);
	}

	private sealed record GamePage(GameDto[] Items, int Total);
}
