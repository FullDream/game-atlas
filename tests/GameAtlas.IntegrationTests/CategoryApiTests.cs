using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameAtlas.Application.Categories;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class CategoryApiTests
{
	[Theory]
	[InlineData("#fff", "#FFFFFF")]
	[InlineData("#F80", "#FF8800")]
	[InlineData("#aBc", "#AABBCC")]
	public async Task ShortColorsPersistExpandedAndEquivalentPatchIsNoOp(string color, string expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();

		var path = CategoryTestData.Path(game.Id, group.Id);
		using var created = await client.PostAsJsonAsync(path, new { name = "Name", iconAssetId = MediaTestData.IconId, color });

		var dto = await ReadAsync(created);

		Assert.Equal(expected, dto.Color);
		Assert.Equal(dto, Assert.Single(await CategoryTestData.LoadAsync(factory, group.Id)));
		using var unchanged = await client.PatchAsJsonAsync(path + $"/{dto.Id}", new { color });

		Assert.Equal(dto, await ReadAsync(unchanged));
		using var changed = await client.PatchAsJsonAsync(path + $"/{dto.Id}", new { color = "#000" });

		var updated = await ReadAsync(changed);

		Assert.Equal("#000000", updated.Color);
		Assert.True(updated.UpdatedAt > dto.UpdatedAt);
		Assert.Equal(updated, Assert.Single(await CategoryTestData.LoadAsync(factory, group.Id)));
	}

	[Theory]
	[InlineData("{}", 422)]
	[InlineData("{\"name\":null,\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\"}", 422)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\"}", 422)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"red\"}", 422)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\",\"order\":null}", 400)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\",\"order\":0}", 400)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\",\"order\":-1}", 400)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\",\"order\":1.5}", 400)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\",\"order\":\"1\"}", 400)]
	[InlineData("{\"name\":\"Name\",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\",\"order\":2147483648}", 400)]
	public async Task CreationValidatesBeforePersistence(string body, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsync(CategoryTestData.Path(game.Id, group.Id),
			new StringContent(body, Encoding.UTF8, "application/json"));

		Assert.Equal(status, (int)response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		Assert.Empty(await CategoryTestData.LoadAsync(factory, group.Id));
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(200, true)]
	[InlineData(201, false)]
	public async Task StringBoundsApplyToCreateAndPatchWithoutPartialChanges(int nameLength, bool valid)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		var before = await CategoryTestData.LoadAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();

		var path = CategoryTestData.Path(game.Id, group.Id);

		var body = new
		{
			name = " " + new string('n', nameLength) + " ",
			iconAssetId = MediaTestData.IconId,
			color = "#abcdef"
		};

		using var created = await client.PostAsJsonAsync(path, body);
		using var patched = await client.PatchAsJsonAsync(path + $"/{category.Id}",
			body with { name = " " + new string('x', nameLength) + " " });

		Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity, created.StatusCode);
		Assert.Equal(valid ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity, patched.StatusCode);

		if (!valid)
			Assert.Equal(before, await CategoryTestData.LoadAsync(factory, group.Id));
	}

	[Theory]
	[InlineData("/icons/chest.svg")]
	[InlineData("https://example.test/icon.png")]
	[InlineData("https://example.test/icon.webp")]
	[InlineData("https://example.test/icon")]
	public async Task LegacyIconStringsAreRejected(string icon)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, group.Id),
			new { name = "Name", icon, color = "#123456" });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task MalformedRouteGuidsReturn400AndWrongCurrentGroupReturns404()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var otherGroup = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Other", 2);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();

		foreach (var path in new[]
		{
			$"/games/bad/maps/019a0000-0000-7000-8000-000000000001/category-groups/{group.Id}/categories", $"/games/{game.Id}/maps/{MapTestData.MapIdFor(game.Id)}/category-groups/bad/categories"
		})
		{
			using var create = await client.PostAsJsonAsync(path, Body());
			using var list = await client.GetAsync(path);
			using var patch = await client.PatchAsJsonAsync(path + $"/{category.Id}", new { name = "New" });
			using var delete = await client.DeleteAsync(path + $"/{category.Id}");

			Assert.All(new[] { create, patch, delete }, response =>
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
		}

		using var badPatch = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id) + "/bad",
			new { name = "New" });

		using var badDelete = await client.DeleteAsync(CategoryTestData.Path(game.Id, group.Id) + "/bad");
		using var wrongPatch = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, otherGroup.Id, category.Id),
			new { name = "New" });

		using var wrongDelete = await client.DeleteAsync(CategoryTestData.Path(game.Id, otherGroup.Id, category.Id));

		Assert.Equal(HttpStatusCode.BadRequest, badPatch.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, badDelete.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, wrongPatch.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, wrongDelete.StatusCode);
	}

	[Theory]
	[InlineData(GameStatus.Draft)]
	[InlineData(GameStatus.Published)]
	public async Task CreatesNormalizedRepresentationWithServerOwnedMetadata(GameStatus status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var game = await DeleteGameHandlerTests.SeedAsync(factory, "game", status);

		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, group.Id), new
		{
			name = " Ресурсы ",
			iconAssetId = MediaTestData.IconId,
			color = "#ff8800"
		});

		var dto = await ReadAsync(response);

		Assert.Equal(7, dto.Id.Version);
		Assert.Equal(group.Id, dto.CategoryGroupId);
		Assert.Equal("Ресурсы", dto.Name);
		Assert.Equal(MediaTestData.IconId, dto.Icon.Id);
		Assert.Equal("#FF8800", dto.Color);
		Assert.Equal(1, await CategoryTestData.OrderAsync(factory, dto.Id));
		Assert.Equal(0, dto.MarkersCount);
		Assert.Equal(dto.CreatedAt, dto.UpdatedAt);
		Assert.NotEqual(DateTimeOffset.MinValue, dto.CreatedAt);
		Assert.Equal(dto, Assert.Single(await CategoryTestData.LoadAsync(factory, group.Id)));
		using var appended = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, group.Id), Body("Second"));

		Assert.Equal(2, (await CategoryTestData.OrderAsync(factory, (await ReadAsync(appended)).Id)));
		await AssertParentsAsync(factory, game, group);
	}

	[Theory]
	[InlineData("Resources", " resources ")]
	[InlineData("Ресурсы", " ресурсы ")]
	public async Task NamesAreUniqueWithinGroupAndRenamesCheckResultingDestination(string name, string duplicate)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, source) = await CategoryTestData.ParentAsync(factory);

		var destination = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Destination", 2);

		var category = await CategoryTestData.SeedAsync(factory, source.Id, name, 4);

		await CategoryTestData.SeedAsync(factory, destination.Id, name, 4);
		using var client = factory.CreateHttpsClient();

		var path = CategoryTestData.Path(game.Id, source.Id, category.Id);

		var before = await CategoryTestData.LoadAsync(factory, source.Id);
		using var createConflict = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, source.Id),
			Body(duplicate));

		Assert.Equal(HttpStatusCode.Conflict, createConflict.StatusCode);
		using var conflict = await client.PatchAsJsonAsync(path, new { categoryGroupId = destination.Id, beforeId = (Guid?)null });

		Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
		Assert.Contains("name", await conflict.Content.ReadAsStringAsync());
		Assert.Equal(before, await CategoryTestData.LoadAsync(factory, source.Id));
		using var own = await client.PatchAsJsonAsync(path, new { name = duplicate });

		Assert.Equal(duplicate.Trim(), (await ReadAsync(own)).Name);
		using var moved = await client.PatchAsJsonAsync(path, new
		{
			categoryGroupId = destination.Id,
			name = "Unique",
			iconAssetId = MediaTestData.IconId,
			color = "#abcdef"
		});

		var dto = await ReadAsync(moved);

		Assert.Equal(destination.Id, dto.CategoryGroupId);
		Assert.Equal(category.Id, dto.Id);
		Assert.Equal(category.CreatedAt, dto.CreatedAt);
		Assert.Equal(2, await CategoryTestData.OrderAsync(factory, dto.Id));
		Assert.Equal(MediaTestData.IconId, dto.Icon.Id);
		Assert.Equal("#ABCDEF", dto.Color);
		Assert.Empty(await CategoryTestData.LoadAsync(factory, source.Id));
		Assert.Equal(2, (await CategoryTestData.LoadAsync(factory, destination.Id)).Length);
		using var oldPatch = await client.PatchAsJsonAsync(path, new { name = "Other" });
		using var oldDelete = await client.DeleteAsync(path);

		Assert.Equal(HttpStatusCode.NotFound, oldPatch.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, oldDelete.StatusCode);
		using var newPatch = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, destination.Id, category.Id),
			new { beforeId = (Guid?)null });

		Assert.Equal(2, (await CategoryTestData.OrderAsync(factory, (await ReadAsync(newPatch)).Id)));
	}

	[Theory]
	[InlineData("application/json")]
	[InlineData("application/json; charset=utf-8")]
	[InlineData("application/merge-patch+json")]
	[InlineData("application/merge-patch+json; charset=utf-8")]
	public async Task PatchMediaTypesPreserveNoOpsAndOmittedFields(string mediaType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();

		var path = CategoryTestData.Path(game.Id, group.Id, category.Id);

		var before = Assert.Single(await CategoryTestData.LoadAsync(factory, group.Id));
		using var noOp = await PatchAsync(client, path,
			$"{{\"name\":\" Category \",\"iconAssetId\":\"019a0000-0000-7000-8000-000000000111\",\"color\":\"#123456\"," +
			$"\"categoryGroupId\":\"{group.Id}\"}}", mediaType);

		Assert.Equal(before, await ReadAsync(noOp));
		using var changed = await PatchAsync(client, path, "{\"color\":\"#abcdef\"}", mediaType);

		var dto = await ReadAsync(changed);

		Assert.Equal(before.Name, dto.Name);
		Assert.Equal(before.Icon, dto.Icon);
		Assert.Equal(1, await CategoryTestData.OrderAsync(factory, dto.Id));
		Assert.Equal("#ABCDEF", dto.Color);
		Assert.True(dto.UpdatedAt > before.UpdatedAt);
		await AssertParentsAsync(factory, game, group);
	}

	[Theory]
	[InlineData("{}", 422)]
	[InlineData("{\"name\":null}", 422)]
	[InlineData("{\"color\":null}", 422)]
	[InlineData("{\"order\":null}", 400)]
	[InlineData("{\"categoryGroupId\":null}", 422)]
	[InlineData("{\"name\":\"New\",\"order\":0}", 400)]
	[InlineData("{\"name\":\"New\",\"order\":-1}", 400)]
	[InlineData("{\"name\":\" \"}", 422)]
	[InlineData("{\"iconAssetId\":\" \"}", 400)]
	[InlineData("{\"name\":\"New\",\"color\":\"red\"}", 422)]
	[InlineData("{\"color\":\"#123456\\n\"}", 422)]
	[InlineData("null", 400)]
	[InlineData("[]", 400)]
	[InlineData("1", 400)]
	[InlineData("{", 400)]
	[InlineData("{\"name\":1}", 400)]
	[InlineData("{\"iconAssetId\":[]}", 400)]
	[InlineData("{\"color\":false}", 400)]
	[InlineData("{\"order\":1.5}", 400)]
	[InlineData("{\"order\":\"1\"}", 400)]
	[InlineData("{\"order\":2147483648}", 400)]
	[InlineData("{\"categoryGroupId\":\"invalid\"}", 400)]
	[InlineData("{\"id\":null}", 400)]
	[InlineData("{\"gameId\":null}", 400)]
	[InlineData("{\"markersCount\":4}", 400)]
	[InlineData("{\"createdAt\":null}", 400)]
	[InlineData("{\"updatedAt\":null}", 400)]
	[InlineData("{\"unknown\":true}", 400)]
	public async Task InvalidPatchNeverPersistsChanges(string body, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		var before = await CategoryTestData.LoadAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, CategoryTestData.Path(game.Id, group.Id, category.Id), body);

		Assert.Equal(status, (int)response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
		Assert.Equal(before, await CategoryTestData.LoadAsync(factory, group.Id));
	}

	[Theory]
	[InlineData("text/plain")]
	[InlineData("application/json-patch+json")]
	public async Task RejectsUnsupportedPatchMedia(string mediaType)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		var before = await CategoryTestData.LoadAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		using var response = await PatchAsync(client, CategoryTestData.Path(game.Id, group.Id, category.Id),
			"{\"name\":\"New\"}", mediaType);

		Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
		Assert.Equal(before, await CategoryTestData.LoadAsync(factory, group.Id));
	}

	[Fact]
	public async Task ParentScopingAndDestinationLookupsReturn404()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var (otherGame, otherGroup) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		var before = await CategoryTestData.LoadAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();

		foreach (var (gameId, groupId) in new[]
		{
			(Guid.NewGuid(), group.Id), (game.Id, Guid.NewGuid()), (game.Id, otherGroup.Id), (otherGame.Id, group.Id)
		})
		{
			var path = CategoryTestData.Path(gameId, groupId);
			using var create = await client.PostAsJsonAsync(path, Body());
			using var list = await client.GetAsync(path);
			using var patch = await client.PatchAsJsonAsync(path + $"/{category.Id}", new { name = "New" });
			using var delete = await client.DeleteAsync(path + $"/{category.Id}");

			Assert.All(new[] { create, patch, delete }, response =>
				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
		}

		foreach (var destination in new[] { Guid.Empty, Guid.NewGuid(), otherGroup.Id })
		{
			using var response = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, category.Id),
				new { categoryGroupId = destination, name = "New", beforeId = (Guid?)null });

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		foreach (var id in new[] { Guid.NewGuid() })
		{
			using var patch = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, id),
				new { name = "New" });

			using var delete = await client.DeleteAsync(CategoryTestData.Path(game.Id, group.Id, id));

			Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
			Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
		}

		Assert.Equal(before, await CategoryTestData.LoadAsync(factory, group.Id));
	}

	[Fact]
	public async Task TreeChildrenUseDeterministicOrderAndDeletionPreservesSurvivors()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var other = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Other", 2);
		using var client = factory.CreateHttpsClient();

		var path = CategoryTestData.Path(game.Id, group.Id);

		Assert.Empty(Assert.IsType<CategoryDto[]>(await CategoryTestData.ReadTreeCategoriesAsync(client, path)));

		var first = await CategoryTestData.SeedAsync(factory, group.Id, "First", 1);

		var middle = await CategoryTestData.SeedAsync(factory, group.Id, "Middle", 2);

		var tied = await CategoryTestData.SeedAsync(factory, group.Id, "Tied", 2);

		var last = await CategoryTestData.SeedAsync(factory, group.Id, "Last", 3);

		await CategoryTestData.SeedAsync(factory, other.Id, "Other", 1);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await context.Categories.Where(item => item.CategoryGroupId == group.Id && item.Order == 2)
				.ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedAt, middle.CreatedAt));
		}

		var expected = new[] { first.Id }.Concat(new[] { middle.Id, tied.Id }.Order()).Append(last.Id).ToArray();

		var listed = Assert.IsType<CategoryDto[]>(await CategoryTestData.ReadTreeCategoriesAsync(client, path));

		Assert.Equal(expected, listed.Select(item => item.Id));
		Assert.All(listed, item => Assert.Equal(0, item.MarkersCount));
		using var deleted = await client.DeleteAsync(path + $"/{middle.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.Empty(await deleted.Content.ReadAsByteArrayAsync());
		using var repeated = await client.DeleteAsync(path + $"/{middle.Id}");

		Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode);
		Assert.Equal(listed.Where(item => item.Id != middle.Id), await CategoryTestData.LoadAsync(factory, group.Id));
		using var recreated = await client.PostAsJsonAsync(path, Body("Middle"));

		Assert.Equal(4, (await CategoryTestData.OrderAsync(factory, (await ReadAsync(recreated)).Id)));
		await AssertParentsAsync(factory, game, group);
	}

	[Fact]
	public async Task ServerPlacementNormalizesLegacyGapsAndRejectsNumericOrder()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureIconAsync(factory);

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();

		var path = CategoryTestData.Path(game.Id, group.Id);
		using var first = await client.PostAsJsonAsync(path, Body());

		Assert.Equal(1, (await CategoryTestData.OrderAsync(factory, (await ReadAsync(first)).Id)));
		await CategoryTestData.SeedAsync(factory, group.Id, "Maximum", int.MaxValue);
		using var overflow = await client.PostAsJsonAsync(path, Body("Overflow"));

		Assert.Equal(HttpStatusCode.OK, overflow.StatusCode);
		using var explicitOrder = await client.PostAsJsonAsync(path,
			new { name = "Duplicate order", iconAssetId = MediaTestData.IconId, color = "#123456", order = 1 });

		Assert.Equal(HttpStatusCode.BadRequest, explicitOrder.StatusCode);

		var other = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Other", 2);
		using var isolated = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, other.Id), Body());

		Assert.Equal(1, (await CategoryTestData.OrderAsync(factory, (await ReadAsync(isolated)).Id)));
	}

	internal static object Body(string name = "Category") => new { name, iconAssetId = MediaTestData.IconId, color = "#123456" };

	internal static async Task<CategoryDto> ReadAsync(HttpResponseMessage response)
	{
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		Assert.Equal(8, json.RootElement.EnumerateObject().Count());
		Assert.False(json.RootElement.TryGetProperty("order", out _));

		return Assert.IsType<CategoryDto>(await response.Content.ReadFromJsonAsync<CategoryDto>());
	}

	internal static async Task<HttpResponseMessage> PatchAsync(HttpClient client, string path, string json,
		string mediaType = "application/json")
	{
		using var content = new StringContent(json, Encoding.UTF8);

		content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType);

		return await client.PatchAsync(path, content);
	}

	private static async Task AssertParentsAsync(GameApiFactory factory, Game game,
		GameAtlas.Domain.Categories.CategoryGroup group)
	{
		await using var scope = factory.Services.CreateAsyncScope();

		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var storedGame = await context.Games.SingleAsync(item => item.Id == game.Id);

		var storedGroup = await context.CategoryGroups.SingleAsync(item => item.Id == group.Id);

		DeleteGameHandlerTests.AssertSameGame(game, storedGame);
		Assert.Equal(group.UpdatedAt, storedGroup.UpdatedAt);
		Assert.Equal(group.CreatedAt, storedGroup.CreatedAt);
	}
}
