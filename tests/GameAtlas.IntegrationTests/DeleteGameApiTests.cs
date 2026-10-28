using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class DeleteGameApiTests
{
	[Theory]
	[InlineData(GameStatus.Draft)]
	[InlineData(GameStatus.Published)]
	public async Task DeletesEitherStatusWithoutAuthenticationAndReturnsEmpty204(GameStatus status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "target", status);
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");
		using var client = factory.CreateHttpsClient();
		using var response = await client.DeleteAsync($"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
		Assert.Empty(await response.Content.ReadAsByteArrayAsync());
		await using var reload = factory.Services.CreateAsyncScope();
		var stored = Assert.Single(await reload.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.ToArrayAsync());

		DeleteGameHandlerTests.AssertSameGame(other, stored);
		using var repeated = await client.DeleteAsync($"/games/{game.Id}");

		await AssertProblemAsync(repeated, HttpStatusCode.NotFound);
	}

	[Theory]
	[InlineData("not-a-guid", HttpStatusCode.BadRequest)]
	[InlineData("00000000-0000-0000-0000-000000000000", HttpStatusCode.NotFound)]
	[InlineData("019a0000-0000-7000-8000-000000000001", HttpStatusCode.NotFound)]
	public async Task RejectsInvalidAndMissingIdentifiersWithoutChangingStorage(string id, HttpStatusCode expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "existing");
		using var client = factory.CreateHttpsClient();
		using var response = await client.DeleteAsync($"/games/{id}");

		await AssertProblemAsync(response, expected);
		await using var reload = factory.Services.CreateAsyncScope();
		var stored = Assert.Single(await reload.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.ToArrayAsync());

		DeleteGameHandlerTests.AssertSameGame(game, stored);
	}

	[Fact]
	public async Task RemovesGameFromListAndPreventsFurtherUpdates()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "target");
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");
		using var client = factory.CreateHttpsClient();
		using var deletion = await client.DeleteAsync($"/games/{game.Id}");
		using var listing = await client.GetAsync("/games");
		using var page = JsonDocument.Parse(await listing.Content.ReadAsStringAsync());

		Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
		Assert.Equal(HttpStatusCode.OK, listing.StatusCode);
		Assert.Equal(1, page.RootElement.GetProperty("total").GetInt32());
		var item = Assert.Single(page.RootElement.GetProperty("items").EnumerateArray());

		Assert.Equal(other.Id, item.GetProperty("id").GetGuid());
		using var update = await client.PatchAsJsonAsync($"/games/{game.Id}", new { name = "Renamed" });

		await AssertProblemAsync(update, HttpStatusCode.NotFound);
		await using var reload = factory.Services.CreateAsyncScope();
		var stored = Assert.Single(await reload.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.ToArrayAsync());

		DeleteGameHandlerTests.AssertSameGame(other, stored);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ReleasesSlugForCreationOrUpdatingAnotherGame(bool update)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "available");
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");
		using var client = factory.CreateHttpsClient();
		using var deletion = await client.DeleteAsync($"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
		using var response = update
			? await client.PatchAsJsonAsync($"/games/{other.Id}", new { slug = "available" })
			: await client.PostAsJsonAsync("/games", new { name = "New", slug = "available", imageAssetId = MediaTestData.CoverId });
		using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		var id = body.RootElement.GetProperty("id").GetGuid();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("available", body.RootElement.GetProperty("slug").GetString());
		Assert.NotEqual(game.Id, id);

		if (update)
			Assert.Equal(other.Id, id);

		await using var reload = factory.Services.CreateAsyncScope();
		var stored = await reload.ServiceProvider.GetRequiredService<ApplicationDbContext>().Games.ToArrayAsync();

		Assert.DoesNotContain(stored, item => item.Id == game.Id);
		Assert.Equal(id, Assert.Single(stored, item => item.Slug == "available").Id);

		if (!update)
			DeleteGameHandlerTests.AssertSameGame(other, stored.Single(item => item.Id == other.Id));
	}

	[Fact]
	public async Task StorageFailureReturnsSafe500AndPreservesGame()
	{
		await using var factory = new GameApiFactory(interceptor: new DeleteFailureInterceptor());

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "target");
		using var client = factory.CreateHttpsClient();
		using var response = await client.DeleteAsync($"/games/{game.Id}");

		await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
		var content = await response.Content.ReadAsStringAsync();

		Assert.DoesNotContain("private deletion failure", content);
		Assert.DoesNotContain("DELETE", content);
		Assert.DoesNotContain("InvalidOperationException", content);
		await using var reload = factory.Services.CreateAsyncScope();
		var stored = Assert.Single(await reload.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.ToArrayAsync());

		DeleteGameHandlerTests.AssertSameGame(game, stored);
	}

	private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
	{
		Assert.Equal(status, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		Assert.Equal((int)status, body.RootElement.GetProperty("status").GetInt32());
	}

	private sealed class DeleteFailureInterceptor : DbCommandInterceptor
	{
		public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
			DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
			CancellationToken cancellationToken = default)
		{
			if (command.CommandText.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("private deletion failure: " + command.CommandText);

			return ValueTask.FromResult(result);
		}
	}
}
