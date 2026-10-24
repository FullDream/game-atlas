using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Media;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MediaDeletionTests
{
	[Fact]
	public async Task PreviewProtectsImageAndDeletionAfterDetachmentRemovesOnlyItsOriginal()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var upload = new MultipartFormDataContent();

		upload.Add(new StringContent("<svg xmlns='http://www.w3.org/2000/svg'/>"), "file", "preview.svg");
		using var response = await client.PostAsync("/media/assets", upload);
		var image = Assert.IsType<ImageAssetDto>(await response.Content.ReadFromJsonAsync<MediaAssetDto>());
		var video = VideoAsset.Create(Guid.CreateVersion7(), "Video", null, "video/mp4", 10, image.Id).Value!;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			context.MediaAssets.Add(video);
			await context.SaveChangesAsync();
		}

		using var protectedDelete = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.Conflict, protectedDelete.StatusCode);
		using var detach = await client.PatchAsJsonAsync($"/media/assets/{video.Id}", new { previewAssetId = (Guid?)null });

		Assert.Equal(HttpStatusCode.OK, detach.StatusCode);
		using var deleted = await client.DeleteAsync($"/media/assets/{image.Id}");

		Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
		Assert.False(File.Exists(Path.Combine(factory.MediaStoragePath, "assets", image.Id.ToString("N"))));
		using var repeated = await client.DeleteAsync($"/media/assets/{image.Id}");
		using var original = await client.GetAsync(image.Url);

		Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, original.StatusCode);
		using var missingVideo = await client.DeleteAsync($"/media/assets/{video.Id}");

		Assert.Equal(HttpStatusCode.NoContent, missingVideo.StatusCode);
	}
}
