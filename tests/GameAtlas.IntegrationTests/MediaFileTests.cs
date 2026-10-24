using System.Net;
using System.Net.Http.Headers;
using GameAtlas.Application.Media.Storage;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public sealed class MediaFileTests
{
	[Fact]
	public async Task StaticDeliverySupportsHeadValidatorsAndVideoRangesWithoutDatabaseReads()
	{
		var capture = new SqlReadCapture();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var bytes = Enumerable.Range(0, 100).Select(value => (byte)value).ToArray();

		"ftypisom"u8.CopyTo(bytes.AsSpan(4));
		var video = VideoAsset.Create(Guid.CreateVersion7(), "Video", null, "video/mp4", bytes.Length).Value!;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var storage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();
			var path = storage.AssetPath(video.Id);

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			await File.WriteAllBytesAsync(path, bytes);
		}

		capture.Enabled = true;
		using var complete = await client.GetAsync($"/media/files/{video.Id}");

		Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
		Assert.Equal(bytes, await complete.Content.ReadAsByteArrayAsync());
		Assert.Equal("video/mp4", complete.Content.Headers.ContentType!.MediaType);
		Assert.Equal("nosniff", Assert.Single(complete.Headers.GetValues("X-Content-Type-Options")));
		Assert.True(complete.Headers.CacheControl!.Public);
		using var headRequest = new HttpRequestMessage(HttpMethod.Head, $"/media/files/{video.Id}");
		using var head = await client.SendAsync(headRequest);

		Assert.Equal(HttpStatusCode.OK, head.StatusCode);
		Assert.Equal(bytes.Length, head.Content.Headers.ContentLength);
		Assert.Empty(await head.Content.ReadAsByteArrayAsync());
		using var conditional = new HttpRequestMessage(HttpMethod.Get, $"/media/files/{video.Id}");

		conditional.Headers.IfNoneMatch.Add(complete.Headers.ETag!);
		using var unchanged = await client.SendAsync(conditional);

		Assert.Equal(HttpStatusCode.NotModified, unchanged.StatusCode);
		using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, $"/media/files/{video.Id}");

		rangeRequest.Headers.Range = new RangeHeaderValue(10, 19);
		using var range = await client.SendAsync(rangeRequest);

		Assert.Equal(HttpStatusCode.PartialContent, range.StatusCode);
		Assert.Equal(bytes[10..20], await range.Content.ReadAsByteArrayAsync());
		using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, $"/media/files/{video.Id}");

		invalidRequest.Headers.Range = new RangeHeaderValue(100, 110);
		using var invalid = await client.SendAsync(invalidRequest);

		Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, invalid.StatusCode);
		File.Delete(Path.Combine(factory.MediaStoragePath, "assets", video.Id.ToString("N")));
		using var missing = await client.GetAsync($"/media/files/{video.Id}");

		Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
		Assert.Empty(capture.Commands);
	}

	[Theory]
	[InlineData("/media/files")]
	[InlineData("/media/files/")]
	[InlineData("/media/files/staging")]
	[InlineData("/media/files/00000000-0000-0000-0000-000000000000")]
	[InlineData("/media/files/assets/anything")]
	public async Task StaticDeliveryRejectsNonAssetPaths(string url)
	{
		await using var factory = new GameApiFactory();
		using var client = factory.CreateHttpsClient();
		using var response = await client.GetAsync(url);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task SvgSecurityHeadersRemainOnConditionalResponses()
	{
		var capture = new SqlReadCapture();
		await using var factory = new GameApiFactory(interceptor: capture);

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var bytes = "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray();
		var image = ImageAsset.Create(Guid.CreateVersion7(), "SVG", null, "image/svg+xml", bytes.Length, null, null)
			.Value!;

		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var storage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();
			var path = storage.AssetPath(image.Id);

			context.MediaAssets.Add(image);
			await context.SaveChangesAsync();
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			await File.WriteAllBytesAsync(path, bytes);
		}

		using var response = await client.GetAsync($"/media/files/{image.Id}");
		var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));

		Assert.Contains("sandbox", policy);
		Assert.Contains("default-src 'none'", policy);
		using var request = new HttpRequestMessage(HttpMethod.Head, $"/media/files/{image.Id}");

		request.Headers.IfNoneMatch.Add(response.Headers.ETag!);
		using var unchanged = await client.SendAsync(request);

		Assert.Equal(HttpStatusCode.NotModified, unchanged.StatusCode);
		Assert.Equal(policy, Assert.Single(unchanged.Headers.GetValues("Content-Security-Policy")));
	}
}
