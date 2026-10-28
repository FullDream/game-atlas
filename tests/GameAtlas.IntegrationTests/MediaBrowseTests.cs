using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameAtlas.Application.Common.Pagination;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MediaBrowseTests
{
	[Fact]
	public async Task ListingIncludesCurrentFolderAndAncestorsIndependentlyOfSearchAndPagination()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var top = await MediaFolderTests.CreateAsync(client, "Top");
		var middleResponse = await client.PostAsJsonAsync("/media/folders", new { name = "Middle", parentId = top.Id });
		var middle = await middleResponse.Content.ReadFromJsonAsync<MediaFolderDto>();
		var leafResponse = await client.PostAsJsonAsync("/media/folders", new { name = "Leaf", parentId = middle!.Id });
		var leaf = await leafResponse.Content.ReadFromJsonAsync<MediaFolderDto>();

		await client.PostAsJsonAsync("/media/folders", new { name = "Child", parentId = leaf!.Id });
		var listing = await client.GetFromJsonAsync<MediaListResult>(
			$"/media?folderId={leaf.Id}&search=absent&offset=100");

		Assert.Empty(listing!.Items);
		Assert.Equal(0, listing.Total);
		Assert.Equal(leaf.Id, listing.Folder!.Id);
		Assert.Equal(1, listing.Folder.ItemsCount);
		Assert.Equal([top.Id, middle.Id], listing.Ancestors.Select(parent => parent.Id));
		Assert.Equal(["Top", "Middle"], listing.Ancestors.Select(parent => parent.Name));
		var root = await client.GetFromJsonAsync<MediaListResult>("/media");

		Assert.Null(root!.Folder);
		Assert.Empty(root.Ancestors);
		using var missing = await client.GetAsync($"/media?folderId={Guid.CreateVersion7()}");

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
	}

	[Fact]
	public async Task LargerMixedPagesKeepQueryCountConstantAndSearchTreatsWildcardsLiterally()
	{
		var capture = new SqlReadCapture();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var folder = await MediaFolderTests.CreateAsync(client, "Folder");
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var image = ImageAsset.Create(Guid.CreateVersion7(), "literal%_value", null, "image/png", 64, 8, 8).Value!;

		context.MediaAssets.Add(image);

		for (var index = 0; index < 30; index++)
		{
			context.MediaAssets.Add(VideoAsset.Create(Guid.CreateVersion7(), $"Video {index:D2}", null,
				"video/mp4", 64, image.Id).Value!);
			context.MediaAssets.Add(ImageAsset.Create(Guid.CreateVersion7(), $"Child {index}", folder.Id,
				"image/png", 64, 8, 8).Value!);
		}

		await context.SaveChangesAsync();
		capture.Enabled = true;
		using var small = await client.GetAsync("/media?limit=2");

		small.EnsureSuccessStatusCode();
		var smallCount = capture.Commands.Count;

		capture.Commands.Clear();
		using var large = await client.GetAsync("/media?limit=100");
		var result = await large.Content.ReadFromJsonAsync<PaginatedResult<MediaItemDto>>();

		Assert.Equal(32, result!.Total);
		Assert.Equal(30, Assert.IsType<MediaFolderDto>(result.Items.First()).ItemsCount);
		Assert.Equal(smallCount, capture.Commands.Count);
		Assert.InRange(smallCount, 1, 5);
		using var literal = await client.GetAsync("/media?search=%25_");
		var matched = await literal.Content.ReadFromJsonAsync<PaginatedResult<MediaItemDto>>();

		Assert.Equal(image.Id, Assert.IsType<ImageAssetDto>(Assert.Single(matched!.Items)).Id);
	}

	[Theory]
	[InlineData("offset=-1", 422)]
	[InlineData("limit=0", 422)]
	[InlineData("limit=101", 422)]
	[InlineData("offset=invalid", 400)]
	[InlineData("limit=invalid", 400)]
	[InlineData("offset=2147483647&limit=100", 200)]
	public async Task PaginationValidatesBoundaries(string query, int expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync($"/media?{query}");

		Assert.Equal(expected, (int)response.StatusCode);
	}

	[Fact]
	public async Task BrowsePagesMixedAssetsAfterFoldersAndCountsImmediateChildrenRegardlessOfSearch()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var folder = await MediaFolderTests.CreateAsync(client, "Z folder");
		var nested = await MediaFolderTests.CreateAsync(client, "Inside", folder.Id);
		var image = ImageAsset.Create(Guid.CreateVersion7(), "B image", null, "image/png", 10, 16, 16,
			"<script>plain text</script>").Value!;
		var video = VideoAsset.Create(Guid.CreateVersion7(), "A video", null, "video/mp4", 20, image.Id).Value!;
		var child = ImageAsset.Create(Guid.CreateVersion7(), "Missing original", folder.Id, "image/png", 10, 16, 16)
			.Value!;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			context.MediaAssets.AddRange(image, video, child);
			await context.SaveChangesAsync();
		}

		using var response = await client.GetAsync("/media?limit=2");
		var page = await response.Content.ReadFromJsonAsync<PaginatedResult<MediaItemDto>>();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(3, page!.Total);
		Assert.Equal(2, page.Items.Count);
		var first = Assert.IsType<MediaFolderDto>(page.Items.First());
		var second = Assert.IsType<VideoAssetDto>(page.Items.Last());

		Assert.Equal(2, first.ItemsCount);
		Assert.Equal(image.Id, second.Preview!.Id);
		Assert.Equal(image.Alt, second.Preview.Alt);
		using var tail = await client.GetAsync("/media?offset=2&limit=2");
		var final = await tail.Content.ReadFromJsonAsync<PaginatedResult<MediaItemDto>>();

		Assert.Equal(image.Id, Assert.IsType<ImageAssetDto>(Assert.Single(final!.Items)).Id);
		using var search = await client.GetAsync("/media?search=z%20FOLDER");
		var filtered = await search.Content.ReadFromJsonAsync<PaginatedResult<MediaItemDto>>();

		Assert.Equal(1, filtered!.Total);
		Assert.Equal(2, Assert.IsType<MediaFolderDto>(Assert.Single(filtered.Items)).ItemsCount);
		using var local = await client.GetAsync($"/media?folderId={nested.Id}&search=Missing");
		var empty = await local.Content.ReadFromJsonAsync<PaginatedResult<MediaItemDto>>();

		Assert.Empty(empty!.Items);
		using var missing = await client.GetAsync($"/media?folderId={Guid.NewGuid()}");

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
	}

	[Fact]
	public async Task AssetMetadataUpdatesPreserveBytesAndNoOpTimestampsAndValidateSubtypes()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var folder = await MediaFolderTests.CreateAsync(client, "Images");
		var image = ImageAsset.Create(Guid.CreateVersion7(), "Original", folder.Id, "image/png", 10, 32, 32).Value!;
		var video = VideoAsset.Create(Guid.CreateVersion7(), "Video", null, "video/webm", 20).Value!;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			context.MediaAssets.AddRange(image, video);
			await context.SaveChangesAsync();
		}

		using var edited = await client.PatchAsJsonAsync($"/media/assets/{image.Id}",
			new { name = " Renamed ", folderId = (Guid?)null, alt = "shared" });
		var dto = await edited.Content.ReadFromJsonAsync<MediaAssetDto>();
		var updated = Assert.IsType<ImageAssetDto>(dto);

		Assert.Equal("Renamed", updated.Name);
		Assert.Equal("shared", updated.Alt);
		Assert.Null(updated.FolderId);
		Assert.Equal(image.ByteLength, updated.ByteLength);
		using var noOp = await client.PatchAsJsonAsync($"/media/assets/{image.Id}", new { name = "Renamed" });
		var unchanged = await noOp.Content.ReadFromJsonAsync<MediaAssetDto>();

		Assert.Equal(updated.UpdatedAt, unchanged!.UpdatedAt);
		using var preview = await client.PatchAsJsonAsync($"/media/assets/{video.Id}", new { previewAssetId = image.Id });
		var attached = Assert.IsType<VideoAssetDto>(await preview.Content.ReadFromJsonAsync<MediaAssetDto>());

		Assert.Equal("shared", attached.Preview!.Alt);
		using var invalid = await client.PatchAsJsonAsync($"/media/assets/{video.Id}", new { alt = "invalid" });

		Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
		using var readOnly = await client.PatchAsJsonAsync($"/media/assets/{image.Id}", new { placeholder = "invalid" });

		Assert.Equal(HttpStatusCode.BadRequest, readOnly.StatusCode);
	}
}
