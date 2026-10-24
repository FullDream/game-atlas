using System.Net;
using System.Net.Http.Json;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MediaReferenceRaceTests
{
	[Fact]
	public async Task ConcurrentFolderDeletionAndAdmissionCannotOrphanTheUploadedAsset()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var folder = await MediaFolderTests.CreateAsync(client, "Destination");
		using var body = new MultipartFormDataContent();

		body.Add(new StringContent("<svg xmlns='http://www.w3.org/2000/svg'/>"), "file", "icon.svg");
		body.Add(new StringContent(folder.Id.ToString()), "folderId");
		var responses = await Task.WhenAll(client.PostAsync("/media/assets", body),
			client.DeleteAsync($"/media/folders/{folder.Id}"));

		using var uploaded = responses[0];
		using var deleted = responses[1];

		Assert.True(uploaded.StatusCode == HttpStatusCode.OK && deleted.StatusCode == HttpStatusCode.Conflict ||
			uploaded.StatusCode == HttpStatusCode.NotFound && deleted.StatusCode == HttpStatusCode.NoContent);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Equal(uploaded.IsSuccessStatusCode, await context.MediaAssets.AnyAsync());
		Assert.Equal(0, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").SingleAsync());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DeleteAndAttachSerializeWithoutDanglingCategoryOrPreviewReferences(bool preview)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await MediaTestData.EnsureMarkerAssetsAsync(factory);
		var (game, group) = await CategoryTestData.ParentAsync(factory);
		var category = await CategoryTestData.SeedAsync(factory, group.Id);
		using var client = factory.CreateHttpsClient();
		var image = await MediaTestData.UploadImageAsync(client, "svg");
		var attaching = preview
			? client.PatchAsJsonAsync($"/media/assets/{MediaTestData.VideoId}", new { previewAssetId = image.Id })
			: client.PatchAsJsonAsync(CategoryTestData.Path(game.Id, group.Id) + $"/{category.Id}",
				new { iconAssetId = image.Id });
		var deleting = client.DeleteAsync($"/media/assets/{image.Id}");
		var responses = await Task.WhenAll(attaching, deleting);

		using var attached = responses[0];
		using var deleted = responses[1];

		Assert.True(attached.StatusCode == HttpStatusCode.OK && deleted.StatusCode == HttpStatusCode.Conflict ||
			attached.StatusCode == HttpStatusCode.NotFound && deleted.StatusCode == HttpStatusCode.NoContent);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.Equal(attached.IsSuccessStatusCode, await context.MediaAssets.AnyAsync(asset => asset.Id == image.Id));
		Assert.Equal(0, await context.Database.SqlQueryRaw<int>(
			"SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").SingleAsync());
	}
}
