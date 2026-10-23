using GameAtlas.Application.CategoryGroups;
using GameAtlas.Domain.CategoryGroups;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

internal static class CategoryGroupTestData
{
	internal static async Task<CategoryGroup> SeedAsync(GameApiFactory factory, Guid gameId, string name, int order)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var result = CategoryGroup.Create(gameId, name, order);

		Assert.True(result.IsSuccess);
		context.CategoryGroups.Add(result.Value);
		await context.SaveChangesAsync();

		return result.Value;
	}

	internal static async Task<CategoryGroupDto[]> LoadAsync(GameApiFactory factory, Guid gameId)
	{
		await using var scope = factory.Services.CreateAsyncScope();
		var groups = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CategoryGroups
			.AsNoTracking().Where(group => group.GameId == gameId)
			.OrderBy(group => group.Order).ThenBy(group => group.CreatedAt).ThenBy(group => group.Id).ToArrayAsync();

		return groups.Select(group => new CategoryGroupDto(group.Id, group.GameId, group.Name, group.Order,
			group.CreatedAt, group.UpdatedAt)).ToArray();
	}
}
