using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Games;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Maps;
using GameAtlas.Application.Maps.GetPublicMap;
using GameAtlas.Domain;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Maps;
using GameAtlas.Domain.Markers;
using GameAtlas.Domain.ValueObjects;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GameAtlas.IntegrationTests;

public class PublicReadTests(ITestOutputHelper output)
{
	[Fact]
	public async Task CatalogFiltersBeforePagingAndKeepsManagementReadsUnchanged()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await DeleteGameHandlerTests.SeedAsync(factory, "draft");
		var first = await DeleteGameHandlerTests.SeedAsync(factory, "first", PublicationStatus.Published);
		var second = await DeleteGameHandlerTests.SeedAsync(factory, "second", PublicationStatus.Published);
		using var client = factory.CreateGuestClient();

		var page = (await client.GetFromJsonAsync<PaginatedResult<PublicGameCardDto>>(
			"/public/games?sortBy=NAME&limit=1&offset=1"))!;

		Assert.Equal(2, page.Total);
		Assert.Equal(new[] { first.Id, second.Id }.Order().Last(), Assert.Single(page.Items).Id);
		Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<PublicGameCardDto>>("/public/games?offset=10"))!.Items);
		Assert.Empty((await client.GetFromJsonAsync<PaginatedResult<PublicGameCardDto>>("/public/games?search=%25"))!.Items);
		Assert.Equal(2, (await client.GetFromJsonAsync<PaginatedResult<PublicGameCardDto>>("/public/games?search=+gAmE+"))!.Total);
		using var administrator = await factory.CreateHttpsClientAsync();

		Assert.Equal(3, (await administrator.GetFromJsonAsync<JsonElement>("/games")).GetProperty("total").GetInt32());
		var json = await client.GetFromJsonAsync<JsonElement>("/public/games");
		var card = json.GetProperty("items")[0];

		Assert.False(card.TryGetProperty("status", out _));
		Assert.False(card.TryGetProperty("maps", out _));
		Assert.False(card.TryGetProperty("description", out _));
		Assert.Equal("image", card.GetProperty("cover").GetProperty("type").GetString());
	}

	[Theory]
	[InlineData("offset=-1", 422)]
	[InlineData("limit=0", 422)]
	[InlineData("limit=101", 422)]
	[InlineData("sortDirection=desc", 422)]
	[InlineData("sortBy=99", 422)]
	[InlineData("sortBy=slug", 400)]
	[InlineData("offset=abc", 400)]
	public async Task PublicListValidatesQuery(string query, int status)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateGuestClient();
		using var response = await client.GetAsync("/public/games?" + query);

		Assert.Equal(status, (int)response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
	}

	[Fact]
	public async Task HierarchyVisibilityControlsDetailsCountsAndInternalPreview()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "visible", PublicationStatus.Published);
		var draftGame = await DeleteGameHandlerTests.SeedAsync(factory, "hidden");
		await using var scope = factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var map = Map.Create(game.Id, "World", Slug.Create("world").Value!, PublicationStatus.Published).Value!;
		var hiddenMap = Map.Create(game.Id, "Draft", Slug.Create("draft").Value!).Value!;
		var hiddenParentMap = Map.Create(draftGame.Id, "World", Slug.Create("world").Value!, PublicationStatus.Published).Value!;

		db.Maps.AddRange(map, hiddenMap, hiddenParentMap);
		var visibleGroup = CategoryGroup.Create(map.Id, "Visible", 2, status: PublicationStatus.Published).Value!;
		var hiddenGroup = CategoryGroup.Create(map.Id, "Hidden", 1).Value!;
		var emptyGroup = CategoryGroup.Create(map.Id, "Empty", 3, status: PublicationStatus.Published).Value!;

		db.CategoryGroups.AddRange(visibleGroup, hiddenGroup, emptyGroup);
		var visible = Category.Create(visibleGroup.Id, "Visible", null, null, 2, PublicationStatus.Published).Value!;
		var hidden = Category.Create(visibleGroup.Id, "Hidden", null, null, 1).Value!;
		var hiddenAncestor = Category.Create(hiddenGroup.Id, "Hidden ancestor", null, null, 1, PublicationStatus.Published).Value!;

		db.Categories.AddRange(visible, hidden, hiddenAncestor);
		var point = Position.Create(1, 2).Value!;

		db.Markers.AddRange(Marker.Create(visible.Id, "Visible", point, status: PublicationStatus.Published).Value!,
			Marker.Create(visible.Id, "Draft marker", point).Value!,
			Marker.Create(hidden.Id, "Hidden category", point, status: PublicationStatus.Published).Value!,
			Marker.Create(hiddenAncestor.Id, "Hidden group", point, status: PublicationStatus.Published).Value!);

		await db.SaveChangesAsync();
		using var client = factory.CreateGuestClient();

		var detail = (await client.GetFromJsonAsync<PublicGameDto>("/public/games/visible"))!;

		Assert.Single(detail.Maps);
		Assert.Equal(1, detail.MapsCount);
		Assert.Equal(1, detail.MarkersCount);
		Assert.Equal(1, detail.CategoriesCount);
		Assert.Null(detail.Maps[0].Preview);
		Assert.Equal(1, detail.Maps[0].MarkersCount);
		Assert.Equal(1, detail.Maps[0].CategoriesCount);
		var card = Assert.Single((await client.GetFromJsonAsync<PaginatedResult<PublicGameCardDto>>("/public/games"))!.Items);

		Assert.Equal(detail.MarkersCount, card.MarkersCount);
		Assert.Equal(detail.CategoriesCount, card.CategoriesCount);
		Assert.Equal(detail.MapsCount, card.MapsCount);
		var result = (await client.GetFromJsonAsync<PublicMapDetailsDto>("/public/games/visible/maps/world?includeDrafts=true&visibility=IncludeDrafts"))!;

		Assert.Equal(map.Id, result.Id);
		Assert.Single(result.Markers);
		Assert.Equal(new[] { "Visible", "Empty" }, result.CategoryGroups.Select(group => group.Name));
		Assert.Equal(1, result.CategoryGroups.First().MarkersCount);
		Assert.Equal(0, result.CategoryGroups.Last().MarkersCount);
		Assert.Empty(result.Styles);
		Assert.Null(result.DefaultStyleId);
		Assert.Null(result.Width);
		var handler = new GetPublicMapHandler(db);
		var preview = await handler.Handle(new GetPublicMap("visible", "world", ContentVisibility.IncludeDrafts), default);

		Assert.True(preview.IsSuccess);
		Assert.Equal(4, preview.Value.Markers.Count);
		Assert.Equal(3, preview.Value.CategoryGroups.Count);
		Assert.Equal(4, preview.Value.CategoryGroups.Sum(group => group.MarkersCount));
		Assert.True((await handler.Handle(new GetPublicMap("visible", "draft", ContentVisibility.IncludeDrafts), default)).IsSuccess);
		Assert.True((await handler.Handle(new GetPublicMap("hidden", "world", ContentVisibility.IncludeDrafts), default)).IsSuccess);

		foreach (var path in new[] { "/public/games/hidden", "/public/games/missing", "/public/games/visible/maps/draft",
			"/public/games/hidden/maps/world", "/public/games/visible/maps/missing" })
		{
			using var response = await client.GetAsync(path);

			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		Assert.Equal(game.UpdatedAt, (await db.Games.AsNoTracking().SingleAsync(item => item.Id == game.Id)).UpdatedAt);
		Assert.Equal(map.UpdatedAt, (await db.Maps.AsNoTracking().SingleAsync(item => item.Id == map.Id)).UpdatedAt);
	}

	[Fact]
	public async Task GameSummariesKeepPerMapCountsAndEmptyMaps()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "counts", PublicationStatus.Published);

		await DeleteGameHandlerTests.SeedAsync(factory, "empty", PublicationStatus.Published);
		await using var scope = factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var maps = Enumerable.Range(0, 3).Select(index => Map.Create(game.Id, $"Map {index}",
			Slug.Create($"map-{index}").Value!, PublicationStatus.Published).Value!).ToArray();

		db.Maps.AddRange(maps);

		foreach (var index in new[] { 0, 1 })
		{
			var group = CategoryGroup.Create(maps[index].Id, "Group", 1, status: PublicationStatus.Published).Value!;
			var category = Category.Create(group.Id, "Category", null, null, 1, PublicationStatus.Published).Value!;

			db.CategoryGroups.Add(group);
			db.Categories.Add(category);
			db.Markers.AddRange(Enumerable.Range(0, index + 1).Select(marker => Marker.Create(category.Id,
				$"Marker {marker}", Position.Create(marker, marker).Value!, status: PublicationStatus.Published).Value!));
		}

		await db.SaveChangesAsync();
		using var client = factory.CreateGuestClient();
		var detail = (await client.GetFromJsonAsync<PublicGameDto>("/public/games/counts"))!;

		Assert.Equal(maps.OrderBy(map => map.CreatedAt).ThenBy(map => map.Id).Select(map => map.Id),
			detail.Maps.Select(map => map.Id));

		Assert.Equal(3, detail.MarkersCount);
		Assert.Equal(2, detail.CategoriesCount);
		Assert.Equal(detail.MarkersCount, detail.Maps.Sum(map => map.MarkersCount));
		Assert.Equal(detail.CategoriesCount, detail.Maps.Sum(map => map.CategoriesCount));
		Assert.Equal(0, detail.Maps.Single(map => map.Id == maps[2].Id).MarkersCount);
		Assert.Equal(0, detail.Maps.Single(map => map.Id == maps[2].Id).CategoriesCount);
		Assert.Equal(2, detail.Maps.Single(map => map.Id == maps[1].Id).MarkersCount);
		var empty = (await client.GetFromJsonAsync<PublicGameDto>("/public/games/empty"))!;

		Assert.Empty(empty.Maps);
		Assert.Equal(0, empty.MarkersCount);
		Assert.Equal(0, empty.CategoriesCount);
	}

	[Fact]
	public async Task CompleteMapReadHasBoundedSqlForTwoThousandMarkers()
	{
		var capture = new SqlReadCapture();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		var game = await DeleteGameHandlerTests.SeedAsync(factory, "large", PublicationStatus.Published);
		await using var scope = factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var map = Map.Create(game.Id, "World", Slug.Create("world").Value!, PublicationStatus.Published).Value!;
		var group = CategoryGroup.Create(map.Id, "Locations", 1, status: PublicationStatus.Published).Value!;
		var category = Category.Create(group.Id, "Chests", null, null, 1, PublicationStatus.Published).Value!;

		db.Maps.Add(map);
		db.CategoryGroups.Add(group);
		db.Categories.Add(category);
		db.Markers.Add(Marker.Create(category.Id, "First", Position.Create(0, 0).Value!, status: PublicationStatus.Published).Value!);
		await db.SaveChangesAsync();
		using var client = factory.CreateGuestClient();

		capture.Enabled = true;
		using var small = await client.GetAsync("/public/games/large/maps/world");

		small.EnsureSuccessStatusCode();
		capture.Enabled = false;
		var smallReads = capture.Commands.Count;

		capture.Commands.Clear();

		db.Markers.AddRange(Enumerable.Range(1, 1999).Select(index => Marker.Create(category.Id,
			$"Marker {index}", Position.Create(index, index).Value!, description: "A representative marker description.",
			status: PublicationStatus.Published).Value!));

		await db.SaveChangesAsync();
		capture.Enabled = true;
		using var response = await client.GetAsync("/public/games/large/maps/world");

		response.EnsureSuccessStatusCode();
		capture.Enabled = false;
		var bytes = await response.Content.ReadAsByteArrayAsync();
		var result = JsonSerializer.Deserialize<PublicMapDetailsDto>(bytes, MapTestData.JsonOptions)!;

		Assert.Equal(2000, result.Markers.Count);
		Assert.Equal(2000, Assert.Single(result.CategoryGroups).MarkersCount);
		Assert.All(result.Markers, marker => Assert.Equal(category.Id, marker.CategoryId));
		Assert.Equal(smallReads, capture.Commands.Count);
		Assert.InRange(smallReads, 1, 8);
		output.WriteLine($"2000 markers: {bytes.Length} UTF-8 bytes; {capture.Commands.Count} SQL reads.");
	}
}
