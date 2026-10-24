using GameAtlas.Application.Categories;
using GameAtlas.Domain.Categories;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Domain.Games;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

internal static class CategoryTestData
{
	internal static string Path(Guid gameId, Guid groupId, Guid? id = null) =>
		$"/games/{gameId}/maps/{MapTestData.MapIdFor(gameId)}/category-groups/{groupId}/categories" + (id is null ? "" : $"/{id}");

	internal static async Task<(Game Game, CategoryGroup Group)> ParentAsync(GameApiFactory factory)
	{
		var game = await DeleteGameHandlerTests.SeedAsync(factory, $"game-{Guid.NewGuid():N}");
		var group = await CategoryGroupTestData.SeedAsync(factory, game.Id, "Group", 1);

		return (game, group);
	}

	internal static async Task<Category> SeedAsync(GameApiFactory factory, Guid groupId, string name = "Category",
		int order = 1)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var category = Assert.IsType<Category>(Category.Create(groupId, name, "icon", "#123456", order).Value);

		context.Categories.Add(category);
		await context.SaveChangesAsync();

		return category;
	}

	internal static async Task<CategoryDto[]> LoadAsync(GameApiFactory factory, Guid groupId)
	{
		await using var scope = factory.Services.CreateAsyncScope();

		return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Categories.AsNoTracking()
			.Where(category => category.CategoryGroupId == groupId)
			.OrderBy(category => category.Order).ThenBy(category => category.CreatedAt).ThenBy(category => category.Id)
			.Select(category => new CategoryDto(category.Id, category.CategoryGroupId, category.Name, category.Icon,
				category.Color, category.Order, 0, category.CreatedAt, category.UpdatedAt)).ToArrayAsync();
	}
}
