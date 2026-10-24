using System.Net;
using System.Net.Http.Json;
using GameAtlas.Application.Media;
using GameAtlas.Domain.Media;
using GameAtlas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameAtlas.IntegrationTests;

public class MediaFolderTests
{
	[Fact]
	public async Task ConcurrentOppositeMovesCannotCreateACycle()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var first = await CreateAsync(client, "First");
		var second = await CreateAsync(client, "Second");
		var responses = await Task.WhenAll(
			Task.Run(() => client.PatchAsJsonAsync($"/media/folders/{first.Id}", new { parentId = second.Id })),
			Task.Run(() => client.PatchAsJsonAsync($"/media/folders/{second.Id}", new { parentId = first.Id })));

		try
		{
			Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
			Assert.Single(responses, response => response.StatusCode == HttpStatusCode.UnprocessableEntity);
		}
		finally
		{
			foreach (var response in responses)
				response.Dispose();
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ChildCreationOrMoveRacingWithDeletionCannotLeaveAnOrphan(bool moveExisting)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var parent = await CreateAsync(client, "Parent");
		var child = await CreateAsync(client, "Child");
		var creation = Task.Run(() => moveExisting
			? client.PatchAsJsonAsync($"/media/folders/{child.Id}", new { parentId = parent.Id })
			: client.PostAsJsonAsync("/media/folders", new { name = "New child", parentId = parent.Id }));
		var deletion = Task.Run(() => client.DeleteAsync($"/media/folders/{parent.Id}"));

		await Task.WhenAll(creation, deletion);
		using var created = await creation;
		using var deleted = await deletion;

		Assert.True(created.StatusCode == HttpStatusCode.OK && deleted.StatusCode == HttpStatusCode.Conflict ||
			created.StatusCode == HttpStatusCode.NotFound && deleted.StatusCode == HttpStatusCode.NoContent);
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		Assert.False(await context.MediaFolders.AnyAsync(folder => folder.ParentId != null &&
			!context.MediaFolders.Any(parentFolder => parentFolder.Id == folder.ParentId)));
	}

	[Fact]
	public async Task MissingParentsAndPhysicalFilesDoNotBypassHierarchyRules()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var parent = await CreateAsync(client, "Parent");
		using var missingCreate = await client.PostAsJsonAsync("/media/folders",
			new { name = "Child", parentId = Guid.NewGuid() });
		using var missingMove = await client.PatchAsJsonAsync($"/media/folders/{parent.Id}",
			new { parentId = Guid.NewGuid() });

		Assert.Equal(HttpStatusCode.NotFound, missingCreate.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, missingMove.StatusCode);
		await using (var scope = factory.Services.CreateAsyncScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var image = ImageAsset.Create(Guid.CreateVersion7(), "No physical file", parent.Id,
				"image/png", 10, 10, 10).Value!;

			context.MediaAssets.Add(image);
			await context.SaveChangesAsync();
		}

		using var deleted = await client.DeleteAsync($"/media/folders/{parent.Id}");

		Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
	}

	[Theory]
	[InlineData("application/json; charset=utf-8", 200)]
	[InlineData("application/merge-patch+json; charset=utf-8", 200)]
	[InlineData("application/json-patch+json", 415)]
	[InlineData("text/plain", 415)]
	public async Task FolderPatchUsesSharedMediaTypeRules(string contentType, int expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var folder = await CreateAsync(client, "Original");
		using var body = new StringContent("{\"name\":\"Updated\"}");

		body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
		using var response = await client.PatchAsync($"/media/folders/{folder.Id}", body);

		Assert.Equal(expected, (int)response.StatusCode);
	}

	[Fact]
	public async Task HierarchyNamesCyclesAndEmptyOnlyDeletionAreAtomic()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var root = await CreateAsync(client, " Shared ");
		var child = await CreateAsync(client, "Icons", root.Id);
		var other = await CreateAsync(client, "Other");

		Assert.Equal("Shared", root.Name);
		using var duplicate = await client.PostAsJsonAsync("/media/folders", new { name = " shared " });
		using var duplicateChild = await client.PostAsJsonAsync("/media/folders",
			new { name = " icons ", parentId = root.Id });
		using var cycle = await client.PatchAsJsonAsync($"/media/folders/{root.Id}",
			new { name = "Wrong", parentId = child.Id });
		using var nonempty = await client.DeleteAsync($"/media/folders/{root.Id}");

		Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, duplicateChild.StatusCode);
		Assert.Equal(HttpStatusCode.UnprocessableEntity, cycle.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, nonempty.StatusCode);
		using var moved = await client.PatchAsJsonAsync($"/media/folders/{child.Id}", new { parentId = other.Id });

		Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
		using var removed = await client.DeleteAsync($"/media/folders/{root.Id}");
		using var repeated = await client.DeleteAsync($"/media/folders/{root.Id}");

		Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode);
		using var rootMove = await client.PatchAsJsonAsync($"/media/folders/{child.Id}", new { parentId = (Guid?)null });
		var updated = (await rootMove.Content.ReadFromJsonAsync<MediaFolderDto>())!;

		Assert.Null(updated.ParentId);
		Assert.Equal(child.Id, updated.Id);
		using var noOp = await client.PatchAsJsonAsync($"/media/folders/{child.Id}", new { name = " Icons " });

		Assert.Equal(updated.UpdatedAt, (await noOp.Content.ReadFromJsonAsync<MediaFolderDto>())!.UpdatedAt);
	}

	[Theory]
	[InlineData("{}", 422)]
	[InlineData("{\"name\":null}", 422)]
	[InlineData("{\"name\":\" \"}", 422)]
	[InlineData("{\"id\":null}", 400)]
	[InlineData("[]", 400)]
	public async Task InvalidPatchesLeaveFolderUnchanged(string json, int expected)
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		using var client = factory.CreateHttpsClient();
		var folder = await CreateAsync(client, "Icons");
		using var response = await client.PatchAsync($"/media/folders/{folder.Id}",
			new StringContent(json, System.Text.Encoding.UTF8, "application/merge-patch+json"));

		Assert.Equal(expected, (int)response.StatusCode);
		await using var scope = factory.Services.CreateAsyncScope();
		var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().MediaFolders.SingleAsync();

		Assert.Equal(folder.Name, stored.Name);
		Assert.Equal(folder.UpdatedAt, stored.UpdatedAt);
	}

	[Fact]
	public async Task SqliteEnforcesRootSiblingAndPreviewRestrictionsWithoutUniqueAssetNames()
	{
		await using var factory = new GameApiFactory();

		await factory.MigrateAsync();
		await using var scope = factory.Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var first = MediaFolder.Create("Иконки").Value!;
		var classifier = new SqlitePersistenceFailureClassifier();

		context.MediaFolders.Add(first);
		await context.SaveChangesAsync();
		context.MediaFolders.Add(MediaFolder.Create(" иконки ").Value!);
		var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		Assert.True(classifier.IsMediaFolderNameConflict(duplicate));
		context.ChangeTracker.Clear();
		var other = MediaFolder.Create("Other").Value!;

		context.MediaFolders.Add(other);
		context.MediaFolders.Add(MediaFolder.Create("Иконки", first.Id).Value!);
		context.MediaFolders.Add(MediaFolder.Create("Иконки", other.Id).Value!);
		await context.SaveChangesAsync();
		context.MediaFolders.Add(MediaFolder.Create(" иконки ", first.Id).Value!);
		var nestedDuplicate = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

		Assert.True(classifier.IsMediaFolderNameConflict(nestedDuplicate));
		context.ChangeTracker.Clear();
		var image = ImageAsset.Create(Guid.CreateVersion7(), "Duplicate", first.Id, "image/png", 10, 10, 10).Value!;
		var video = VideoAsset.Create(Guid.CreateVersion7(), "Duplicate", first.Id, "video/mp4", 100, image.Id).Value!;

		context.MediaAssets.AddRange(image, video);
		await context.SaveChangesAsync();

		Assert.Equal(2, await context.MediaAssets.CountAsync());
		await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
			context.MediaAssets.Where(asset => asset.Id == image.Id).ExecuteDeleteAsync());
		await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
			context.MediaFolders.Where(folder => folder.Id == first.Id).ExecuteDeleteAsync());

		Assert.False(context.Database.HasPendingModelChanges());
	}

	internal static async Task<MediaFolderDto> CreateAsync(HttpClient client, string name, Guid? parentId = null)
	{
		using var response = await client.PostAsJsonAsync("/media/folders", new { name, parentId });

		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<MediaFolderDto>())!;
	}
}
