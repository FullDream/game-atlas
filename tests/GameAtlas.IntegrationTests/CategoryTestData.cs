using System.Net.Http.Json;
using GameAtlas.Application.Categories.Groups;
using GameAtlas.Application.Categories;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Media;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

internal static class CategoryTestData
{
	internal static async Task<int> OrderAsync(GameApiFactory factory, Guid id)
	{
		await using var scope = factory.Services.CreateAsyncScope();

		return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Categories
			.Where(item => item.Id == id).Select(item => item.Order).SingleAsync();
	}

	internal static async Task<CategoryDto[]> ReadTreeCategoriesAsync(HttpClient client, string categoryPath)
	{
		var parts = categoryPath.Split("/category-groups/");

		var groupId = Guid.Parse(parts[1].Split("/")[0]);

		var groups = await client.GetFromJsonAsync<CategoryGroupDto[]>(parts[0] + "/category-groups");

		return groups!.Single(group => group.Id == groupId).Categories.ToArray();
	}

	internal static string Path(Guid gameId, Guid groupId, Guid? id = null) =>
		$"/games/{gameId}/maps/{MapTestData.MapIdFor(gameId)}/category-groups/{groupId}/categories" + (id is null ? "" : $"/{id}");

	internal static async Task<(Game Game, CategoryGroup Group)> ParentAsync(GameApiFactory factory)
	{
		await MediaTestData.EnsureIconAsync(factory);

		var game = await DeleteGameHandlerTests.SeedAsync(factory, $"game-{Guid.NewGuid():N}");

		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);

		return (game, group);
	}

	internal static async Task<Category> SeedAsync(GameApiFactory factory, Guid groupId, string name = "Category",
		int order = 1)
	{
		await MediaTestData.EnsureIconAsync(factory);
		await using var scope = factory.Services.CreateAsyncScope();

		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var category = Assert.IsType<Category>(Category.Create(groupId, name, MediaTestData.IconId, "#123456", order).Value);

		context.Categories.Add(category);
		await context.SaveChangesAsync();

		return category;
	}

	internal static async Task<CategoryDto[]> LoadAsync(GameApiFactory factory, Guid groupId)
	{
		await using var scope = factory.Services.CreateAsyncScope();

		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var categories = await context.Categories.AsNoTracking()
			.Where(category => category.CategoryGroupId == groupId)
			.OrderBy(category => category.Order).ThenBy(category => category.CreatedAt).ThenBy(category => category.Id)
			.ToArrayAsync();

		var ids = categories.Select(category => category.IconAssetId).Distinct().ToArray();

		var images = await context.MediaAssets.OfType<ImageAsset>().Where(image => ids.Contains(image.Id))
			.ToDictionaryAsync(image => image.Id);

		return categories.Select(category => new CategoryDto(category.Id, category.CategoryGroupId, category.Name,
			category.IconAssetId is { } iconId ? images[iconId].ToDto() : null, category.Color, 0,
			category.CreatedAt, category.UpdatedAt)).ToArray();
	}
}
