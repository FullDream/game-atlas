using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CategoryGroupApiTests
{
	[Theory]
	[InlineData("GET", "/games/not-a-guid/category-groups")]
	[InlineData("POST", "/games/not-a-guid/category-groups")]
	[InlineData("PATCH", "/games/019a0000-0000-7000-8000-000000000001/category-groups/not-a-guid")]
	[InlineData("DELETE", "/games/019a0000-0000-7000-8000-000000000001/category-groups/not-a-guid")]
	public async Task MalformedRouteIdentifiersReturn400(string method, string path)
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateHttpsClient();
		using var request = new HttpRequestMessage(new HttpMethod(method), path);

		if (method is "POST" or "PATCH")
			request.Content = JsonContent.Create(new { name = "Group" });

		using var response = await client.SendAsync(request);

		await AssertProblemAsync(response, HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData(GameStatus.Draft)]
	[InlineData(GameStatus.Published)]
	public async Task CreatesAndListsCompleteGroupsForEitherGameStatus(GameStatus status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game", status);
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");

		await CategoryGroupTestData.SeedAsync(factory, other.Id, "Other", 1);
		using var client = factory.CreateHttpsClient();
		using var empty = await client.GetAsync(Path(game.Id));

		Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
		Assert.Equal("[]", await empty.Content.ReadAsStringAsync());
		using var response = await client.PostAsJsonAsync(Path(game.Id), new
		{
			name = " Ресурсы ",
			id = Guid.NewGuid(),
			gameId = other.Id,
			order = 99,
			createdAt = DateTimeOffset.MinValue,
			updatedAt = DateTimeOffset.MinValue
		});

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		var group = await response.Content.ReadFromJsonAsync<CategoryGroupDto>();

		Assert.NotNull(group);
		Assert.Equal(6, json.RootElement.EnumerateObject().Count());
		Assert.Equal(7, group.Id.Version);
		Assert.Equal(game.Id, group.GameId);
		Assert.Equal("Ресурсы", group.Name);
		Assert.Equal(99, group.Order);
		Assert.Equal(group.CreatedAt, group.UpdatedAt);
		Assert.Equal(TimeSpan.Zero, group.CreatedAt.Offset);
		using var next = await client.PostAsJsonAsync(Path(game.Id), new { name = "Locations" });

		Assert.Equal(HttpStatusCode.OK, next.StatusCode);
		using var listing = await client.GetAsync(Path(game.Id));
		var items = await listing.Content.ReadFromJsonAsync<CategoryGroupDto[]>();

		Assert.Equal(new[] { "Ресурсы", "Locations" }, items!.Select(item => item.Name));
		Assert.Equal(new[] { 99, 100 }, items.Select(item => item.Order));
		Assert.Equal(group, items[0]);
		Assert.Equal(items, await CategoryGroupTestData.LoadAsync(factory, game.Id));
		await using var scope = factory.Services.CreateAsyncScope();
		var storedGame = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
			.Games.SingleAsync(item => item.Id == game.Id);

		DeleteGameHandlerTests.AssertSameGame(game, storedGame);
	}

	[Theory]
	[InlineData("{}")]
	[InlineData("{\"name\":null}")]
	[InlineData("{\"name\":\" \"}")]
	public async Task InvalidNamesReturn422ForCreateAndRename(string body)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var creation = await client.PostAsync(Path(game.Id),
			new StringContent(body, Encoding.UTF8, "application/json"));

		using var update = await PatchAsync(client, Path(game.Id, group.Id), body);

		await AssertProblemAsync(creation, HttpStatusCode.UnprocessableEntity);
		await AssertProblemAsync(update, HttpStatusCode.UnprocessableEntity);
		Assert.Equal(before, await CategoryGroupTestData.LoadAsync(factory, game.Id));
	}

	[Theory]
	[InlineData(1, HttpStatusCode.OK)]
	[InlineData(200, HttpStatusCode.OK)]
	[InlineData(201, HttpStatusCode.UnprocessableEntity)]
	public async Task NameLengthIsMeasuredAfterTrimming(int length, HttpStatusCode expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsJsonAsync(Path(game.Id), new { name = " " + new string('x', length) + " " });

		Assert.Equal(expected, response.StatusCode);
	}

	[Theory]
	[InlineData("Resources", " resources ")]
	[InlineData("Ресурсы", " ресурсы ")]
	public async Task RejectsScopedConflictsAndAllowsSelfCaseChanges(string name, string duplicate)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, name, 1);
		var second = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Other", 2);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var creation = await client.PostAsJsonAsync(Path(game.Id), new { name = duplicate });
		using var conflict = await client.PatchAsJsonAsync(Path(game.Id, second.Id), new { name = duplicate });

		await AssertProblemAsync(creation, HttpStatusCode.Conflict, "name");
		await AssertProblemAsync(conflict, HttpStatusCode.Conflict, "name");
		Assert.Equal(before, await CategoryGroupTestData.LoadAsync(factory, game.Id));
		using var differentGame = await client.PostAsJsonAsync(Path(other.Id), new { name = duplicate });
		using var self = await client.PatchAsJsonAsync(Path(game.Id, group.Id), new { name = duplicate });
		var changed = await self.Content.ReadFromJsonAsync<CategoryGroupDto>();

		Assert.Equal(HttpStatusCode.OK, differentGame.StatusCode);
		Assert.Equal(HttpStatusCode.OK, self.StatusCode);
		Assert.Equal(duplicate.Trim(), changed!.Name);
		Assert.True(changed.UpdatedAt > group.UpdatedAt);
		Assert.Equal(group.CreatedAt, changed.CreatedAt);
	}

	[Theory]
	[InlineData("application/json")]
	[InlineData("application/merge-patch+json")]
	[InlineData("application/json; charset=utf-8")]
	[InlineData("application/merge-patch+json; charset=utf-8")]
	public async Task RenamePreservesMetadataAndNoOps(string mediaType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Original", 5);
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, Path(game.Id, group.Id), "{\"name\":\" New \"}", mediaType);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var dto = await response.Content.ReadFromJsonAsync<CategoryGroupDto>();

		Assert.Equal("New", dto!.Name);
		Assert.Equal(group.Id, dto.Id);
		Assert.Equal(game.Id, dto.GameId);
		Assert.Equal(5, dto.Order);
		Assert.Equal(group.CreatedAt, dto.CreatedAt);
		Assert.True(dto.UpdatedAt > group.UpdatedAt);
		using var unchanged = await PatchAsync(client, Path(game.Id, group.Id), "{\"name\":\" New \"}", mediaType);

		Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
		Assert.Equal(dto, await unchanged.Content.ReadFromJsonAsync<CategoryGroupDto>());
		Assert.Equal(dto, Assert.Single(await CategoryGroupTestData.LoadAsync(factory, game.Id)));
	}

	[Theory]
	[InlineData("{\"id\":null,\"name\":\"New\"}", 400)]
	[InlineData("{\"gameId\":null}", 400)]
	[InlineData("{\"createdAt\":null}", 400)]
	[InlineData("{\"updatedAt\":null}", 400)]
	[InlineData("{\"unknown\":0}", 400)]
	[InlineData("{\"name\":7}", 400)]
	[InlineData("{", 400)]
	[InlineData("[]", 400)]
	[InlineData("null", 400)]
	[InlineData("1", 400)]
	public async Task RejectsInvalidRenameBodiesWithoutMutation(string body, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, Path(game.Id, group.Id), body);

		await AssertProblemAsync(response, (HttpStatusCode)status);
		Assert.Equal(before, await CategoryGroupTestData.LoadAsync(factory, game.Id));
	}

	[Theory]
	[InlineData("text/plain")]
	[InlineData("application/json-patch+json")]
	public async Task RejectsUnsupportedPatchMediaTypes(string mediaType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, Path(game.Id, group.Id), "{\"name\":\"New\"}", mediaType);

		await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
		Assert.Equal("Group", Assert.Single(await CategoryGroupTestData.LoadAsync(factory, game.Id)).Name);
	}

	[Fact]
	public async Task MissingParentsAndForeignGroupsReturn404()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		var missing = Guid.NewGuid();
		using var client = factory.CreateHttpsClient();
		using var creation = await client.PostAsJsonAsync(Path(missing), new { name = "New" });
		using var listing = await client.GetAsync(Path(missing));
		using var rename = await client.PatchAsJsonAsync(Path(other.Id, group.Id), new { name = "New" });
		using var deletion = await client.DeleteAsync(Path(other.Id, group.Id));
		using var missingGroup = await client.PatchAsJsonAsync(Path(game.Id, missing), new { name = "New" });
		using var missingParent = await client.DeleteAsync(Path(missing, group.Id));

		foreach (var response in new[] { creation, listing, rename, deletion, missingGroup, missingParent })
			await AssertProblemAsync(response, HttpStatusCode.NotFound);

		Assert.Equal("Group", Assert.Single(await CategoryGroupTestData.LoadAsync(factory, game.Id)).Name);
	}

	[Fact]
	public async Task DeletePreservesGapsReleasesNameAndGameDeleteCascades()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game");
		var other = await DeleteGameHandlerTests.SeedAsync(factory, "other");

		await CategoryGroupTestData.SeedAsync(factory, game.Id, "First", 1);
		var middle = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Middle", 2);

		await CategoryGroupTestData.SeedAsync(factory, game.Id, "Last", 3);
		await CategoryGroupTestData.SeedAsync(factory, other.Id, "Other", 1);
		var before = await CategoryGroupTestData.LoadAsync(factory, game.Id);
		var otherBefore = await CategoryGroupTestData.LoadAsync(factory, other.Id);
		using var client = factory.CreateHttpsClient();
		using var deletion = await client.DeleteAsync(Path(game.Id, middle.Id));

		Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
		Assert.Empty(await deletion.Content.ReadAsByteArrayAsync());
		Assert.Equal(before.Where(item => item.Id != middle.Id), await CategoryGroupTestData.LoadAsync(factory, game.Id));
		using var repeated = await client.DeleteAsync(Path(game.Id, middle.Id));

		await AssertProblemAsync(repeated, HttpStatusCode.NotFound);
		using var reuse = await client.PostAsJsonAsync(Path(game.Id), new { name = "Middle" });

		Assert.Equal(HttpStatusCode.OK, reuse.StatusCode);
		Assert.Equal(4, (await reuse.Content.ReadFromJsonAsync<CategoryGroupDto>())!.Order);
		using var gameDeletion = await client.DeleteAsync($"/games/{game.Id}");

		Assert.Equal(HttpStatusCode.NoContent, gameDeletion.StatusCode);
		Assert.Empty(await CategoryGroupTestData.LoadAsync(factory, game.Id));
		Assert.Equal(otherBefore, await CategoryGroupTestData.LoadAsync(factory, other.Id));
	}

	internal static string Path(Guid gameId, Guid? id = null) =>
		$"/games/{gameId}/category-groups" + (id is null ? "" : $"/{id}");

	internal static async Task<HttpResponseMessage> PatchAsync(
		HttpClient client, string path, string json, string mediaType = "application/json")
	{
		using var content = new StringContent(json, Encoding.UTF8);

		content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType);

		return await client.PatchAsync(path, content);
	}

	internal static async Task AssertProblemAsync(
		HttpResponseMessage response, HttpStatusCode expected, string? field = null)
	{
		Assert.Equal(expected, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		Assert.Equal((int)expected, body.RootElement.GetProperty("status").GetInt32());

		if (field is not null)
			Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out _));
	}
}
