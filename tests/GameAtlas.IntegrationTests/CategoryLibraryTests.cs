using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Categories;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class CategoryLibraryTests
{
	[Fact]
	public async Task ResolvingIconsKeepsAConstantQueryCount()
	{
		var capture = new SqlReadCapture();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();

		capture.Enabled = true;
		using var small = await client.GetAsync(CategoryGroupApiTests.Path(game.Id));

		small.EnsureSuccessStatusCode();
		Assert.Equal(4, capture.Commands.Count);
		capture.Enabled = false;

		for (var index = 0; index < 20; index++)
			await CategoryTestData.SeedAsync(factory, group.Id, $"Category {index}");

		capture.Commands.Clear();
		capture.Enabled = true;
		using var larger = await client.GetAsync(CategoryGroupApiTests.Path(game.Id));

		larger.EnsureSuccessStatusCode();
		Assert.Equal(4, capture.Commands.Count);
	}

	[Theory]
	[InlineData("png")]
	[InlineData("jpeg")]
	[InlineData("webp")]
	[InlineData("avif")]
	[InlineData("svg")]
	public async Task UploadedImageFormatsResolveTheSameSharedDtoAndPlaceholder(string format)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();

		var image = await MediaTestData.UploadImageAsync(client, format);
		using var created = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, group.Id),
			new { name = "Category", iconAssetId = image.Id, color = "#fff" });

		var category = await CategoryApiTests.ReadAsync(created);

		Assert.Equal(image, category.Icon);

		var listed = await CategoryTestData.ReadTreeCategoriesAsync(client, CategoryTestData.Path(game.Id, group.Id));

		Assert.Equal(image, Assert.Single(listed!).Icon);
	}

	[Theory]
	[InlineData("not-a-guid", 400)]
	[InlineData("00000000-0000-0000-0000-000000000000", 404)]
	[InlineData("019a0000-0000-7000-8000-000000000999", 404)]
	public async Task CreationRejectsInvalidOrMissingIcons(string? iconAssetId, int expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);
		using var client = factory.CreateHttpsClient();
		using var response = await client.PostAsJsonAsync(CategoryTestData.Path(game.Id, group.Id),
			new { name = "Category", iconAssetId, color = "#fff" });

		Assert.Equal(expected, (int)response.StatusCode);
		Assert.Empty(await CategoryTestData.LoadAsync(factory, group.Id));
	}

	[Fact]
	public async Task SharedIconUpdatesFlowAcrossGamesWithoutChangingCategoryTimestampsAndSurviveDetachment()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var (firstGame, firstGroup) = await CategoryTestData.ParentAsync(factory);

		var (secondGame, secondGroup) = await CategoryTestData.ParentAsync(factory);

		var first = await CategoryTestData.SeedAsync(factory, firstGroup.Id);

		var second = await CategoryTestData.SeedAsync(factory, secondGroup.Id);
		using var client = factory.CreateHttpsClient();
		using var protectedDelete = await client.DeleteAsync($"/media/assets/{MediaTestData.IconId}");

		Assert.Equal(HttpStatusCode.Conflict, protectedDelete.StatusCode);
		using var metadata = await client.PatchAsJsonAsync($"/media/assets/{MediaTestData.IconId}",
			new { name = "Shared renamed icon", alt = "Shared description" });

		Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);

		foreach (var (game, group, category) in new[]
		{
			(firstGame, firstGroup, first), (secondGame, secondGroup, second)
		})
		{
			var result = await CategoryTestData.ReadTreeCategoriesAsync(client, CategoryTestData.Path(game.Id, group.Id));

			var dto = Assert.Single(result!);

			Assert.Equal("Shared renamed icon", dto.Icon.Name);
			Assert.Equal("Shared description", dto.Icon.Alt);
			Assert.Equal(category.UpdatedAt, dto.UpdatedAt);
			Assert.Equal(MediaTestData.IconId, dto.Icon.Id);
		}

		using var deleteFirst = await client.DeleteAsync($"/games/{firstGame.Id}");
		using var stillUsed = await client.DeleteAsync($"/media/assets/{MediaTestData.IconId}");

		Assert.Equal(HttpStatusCode.NoContent, deleteFirst.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, stillUsed.StatusCode);
		using var deleteSecond = await client.DeleteAsync(CategoryTestData.Path(secondGame.Id, secondGroup.Id, second.Id));

		Assert.Equal(HttpStatusCode.NoContent, deleteSecond.StatusCode);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			Assert.True(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().MediaAssets
				.AnyAsync(asset => asset.Id == MediaTestData.IconId));
		}

		using var unusedDelete = await client.DeleteAsync($"/media/assets/{MediaTestData.IconId}");

		Assert.Equal(HttpStatusCode.NoContent, unusedDelete.StatusCode);
	}

	[Fact]
	public async Task MissingAndWrongTypeIconsLeaveCombinedPatchesUnchanged()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();

		var (game, group) = await CategoryTestData.ParentAsync(factory);

		var category = await CategoryTestData.SeedAsync(factory, group.Id);

		var video = VideoAsset.Create(Guid.CreateVersion7(), "Video", null, "video/mp4", 12).Value!;

		var folder = MediaFolder.Create("Folder").Value!;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			context.MediaAssets.Add(video);
			context.MediaFolders.Add(folder);
			await context.SaveChangesAsync();
		}

		using var client = factory.CreateHttpsClient();

		foreach (var (id, expected) in new[] { (video.Id, 422), (folder.Id, 422), (Guid.NewGuid(), 404) })
		{
			using var patch = await client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id, category.Id),
				new { name = "Must not persist", iconAssetId = id });

			Assert.Equal(expected, (int)patch.StatusCode);
			Assert.Equal(category.Name, Assert.Single(await CategoryTestData.LoadAsync(factory, group.Id)).Name);
		}
	}
}
